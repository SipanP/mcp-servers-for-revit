using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Setup;

public enum CheckStatus
{
    Passed,
    Failed,

    /// <summary>True but not required — reported so the picture is complete.</summary>
    Information
}

public sealed record Check(CheckStatus Status, string Title, string? Detail = null);

/// <summary>
/// Reports what is and is not wired up, so a report of "it doesn't work" can be answered with
/// one command's output rather than a conversation.
/// </summary>
public static class DoctorCommand
{
    public static int Run(SetupOptions options, TextWriter output, Func<string, int, bool>? probePort = null)
    {
        var checks = Collect(options, probePort ?? IsPortOpen).ToList();

        output.WriteLine("mcp-servers-for-revit — installation check");
        output.WriteLine();

        foreach (var check in checks)
        {
            var marker = check.Status switch
            {
                CheckStatus.Passed => "[ ok ]",
                CheckStatus.Failed => "[fail]",
                _ => "[info]"
            };

            output.WriteLine($"{marker} {check.Title}");
            if (!string.IsNullOrEmpty(check.Detail))
            {
                output.WriteLine($"       {check.Detail}");
            }
        }

        var failures = checks.Count(check => check.Status is CheckStatus.Failed);
        output.WriteLine();
        output.WriteLine(failures == 0
            ? "Everything looks correct."
            : $"{failures} problem(s) found. Running 'mcp-server-for-revit setup' fixes most of them.");

        return failures == 0 ? 0 : 1;
    }

    internal static IEnumerable<Check> Collect(SetupOptions options, Func<string, int, bool> probePort)
    {
        yield return File.Exists(options.ServerExecutablePath)
            ? new Check(CheckStatus.Passed, "Server executable found", options.ServerExecutablePath)
            : new Check(CheckStatus.Failed, "Server executable is missing", options.ServerExecutablePath);

        var installations = options.SelectInstallations();
        if (installations.Count == 0)
        {
            yield return new Check(CheckStatus.Failed, "No Revit installation found",
                $"Looked under {options.Paths.AutodeskRoot} and {options.Paths.RevitAddinsRoot}");
        }

        foreach (var revit in installations)
        {
            foreach (var check in CheckRevit(revit))
            {
                yield return check;
            }
        }

        foreach (var check in CheckClaudeDesktop(options))
        {
            yield return check;
        }

        var settings = new RevitServerOptions();
        yield return probePort(settings.Host, settings.Port)
            ? new Check(CheckStatus.Information, $"Revit is listening on {settings.Host}:{settings.Port}")
            : new Check(CheckStatus.Information, $"Nothing is listening on {settings.Host}:{settings.Port}",
                "This is expected unless Revit is running with the plugin loaded.");
    }

    private static IEnumerable<Check> CheckRevit(RevitInstallation revit)
    {
        yield return File.Exists(revit.AddinManifestPath)
            ? new Check(CheckStatus.Passed, $"Revit {revit.Year}: add-in manifest installed")
            : new Check(CheckStatus.Failed, $"Revit {revit.Year}: add-in manifest missing", revit.AddinManifestPath);

        var pluginAssembly = Path.Combine(revit.PluginDirectory, "RevitMCPPlugin.dll");
        yield return File.Exists(pluginAssembly)
            ? new Check(CheckStatus.Passed, $"Revit {revit.Year}: plugin installed")
            : new Check(CheckStatus.Failed, $"Revit {revit.Year}: plugin assembly missing", pluginAssembly);

        var commandSetAssembly = Path.Combine(
            revit.CommandSetDirectory, revit.Year.ToString(), $"{RevitInstallations.CommandSetName}.dll");
        yield return File.Exists(commandSetAssembly)
            ? new Check(CheckStatus.Passed, $"Revit {revit.Year}: command set installed")
            : new Check(CheckStatus.Failed, $"Revit {revit.Year}: command set missing", commandSetAssembly);

        yield return CheckCommandRegistry(revit);
    }

    private static Check CheckCommandRegistry(RevitInstallation revit)
    {
        if (!File.Exists(revit.CommandRegistryPath))
        {
            return new Check(CheckStatus.Failed, $"Revit {revit.Year}: no commands enabled", revit.CommandRegistryPath);
        }

        try
        {
            var registry = JsonNode.Parse(File.ReadAllText(revit.CommandRegistryPath))?.AsObject();
            var enabled = registry?["commands"]?.AsArray()
                .Count(command => command?["enabled"]?.GetValue<bool>() == true) ?? 0;

            return enabled > 0
                ? new Check(CheckStatus.Passed, $"Revit {revit.Year}: {enabled} commands enabled")
                : new Check(CheckStatus.Failed, $"Revit {revit.Year}: no commands enabled",
                    "The plugin will start but expose nothing.");
        }
        catch (JsonException ex)
        {
            return new Check(CheckStatus.Failed, $"Revit {revit.Year}: command registry is unreadable", ex.Message);
        }
    }

    private static IEnumerable<Check> CheckClaudeDesktop(SetupOptions options)
    {
        var configPath = options.Paths.ClaudeDesktopConfigFile;
        if (!File.Exists(configPath))
        {
            yield return new Check(CheckStatus.Information, "Claude Desktop is not configured", configPath);
            yield break;
        }

        JsonObject? root;
        string? parseError = null;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(configPath))?.AsObject();
        }
        catch (JsonException ex)
        {
            root = null;
            parseError = ex.Message;
        }

        if (parseError is not null)
        {
            yield return new Check(CheckStatus.Failed, "Claude Desktop configuration is unreadable", parseError);
            yield break;
        }

        var command = root?["mcpServers"]?[ClaudeDesktopConfigurator.ServerKey]?["command"]?.GetValue<string>();

        if (command is null)
        {
            yield return new Check(CheckStatus.Failed, "Claude Desktop does not list this server", configPath);
        }
        else if (!File.Exists(command))
        {
            yield return new Check(CheckStatus.Failed, "Claude Desktop points at a missing executable", command);
        }
        else
        {
            yield return new Check(CheckStatus.Passed, "Claude Desktop is configured", command);
        }
    }

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync(host, port).Wait(TimeSpan.FromSeconds(2)) && client.Connected;
        }
        catch (Exception ex) when (ex is SocketException or AggregateException)
        {
            return false;
        }
    }
}

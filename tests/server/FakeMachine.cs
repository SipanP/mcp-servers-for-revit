using System.Text.Json.Nodes;
using RevitMcpServer.Setup;

namespace RevitMcpServer.Tests;

/// <summary>Records what was asked of an external CLI without launching anything.</summary>
internal sealed class RecordingProcessRunner : IProcessRunner
{
    public List<string> Invocations { get; } = [];

    public ProcessResult Result { get; init; } = new(true, 0, string.Empty);

    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments)
    {
        Invocations.Add($"{fileName} {string.Join(' ', arguments)}");
        return Result;
    }
}

/// <summary>
/// A throwaway directory tree standing in for a machine with Revit and Claude Desktop installed,
/// so the installation flow can be driven end to end without touching the real system.
/// </summary>
internal sealed class FakeMachine : IDisposable
{
    private readonly string _root;

    private FakeMachine(string root, InstallPaths paths, string serverPath)
    {
        _root = root;
        Paths = paths;
        ServerPath = serverPath;
    }

    public InstallPaths Paths { get; }

    /// <summary>A stand-in for the installed executable; the flow only checks that it exists.</summary>
    public string ServerPath { get; }

    public int ManifestCommandCount { get; private set; }

    public static FakeMachine Create(IReadOnlyList<int> revitYears)
    {
        var root = Path.Combine(Path.GetTempPath(), $"revit-mcp-setup-{Guid.NewGuid():N}");
        var paths = new InstallPaths
        {
            RevitAddinsRoot = Path.Combine(root, "AppData", "Autodesk", "Revit", "Addins"),
            AutodeskRoot = Path.Combine(root, "ProgramFiles", "Autodesk"),
            ClaudeDesktopConfigFile = Path.Combine(root, "AppData", "Claude", "claude_desktop_config.json")
        };

        Directory.CreateDirectory(paths.RevitAddinsRoot);
        foreach (var year in revitYears)
        {
            var revitDirectory = Path.Combine(paths.AutodeskRoot, $"Revit {year}");
            Directory.CreateDirectory(revitDirectory);
            File.WriteAllText(Path.Combine(revitDirectory, "Revit.exe"), string.Empty);
        }

        var serverPath = Path.Combine(root, "Programs", "mcp-server-for-revit.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(serverPath)!);
        File.WriteAllText(serverPath, string.Empty);

        return new FakeMachine(root, paths, serverPath);
    }

    /// <summary>Lays out the files the installer would have copied for one Revit version.</summary>
    public void InstallPluginFiles(int year)
    {
        var installation = Installation(year);
        Directory.CreateDirectory(installation.PluginDirectory);
        Directory.CreateDirectory(Path.Combine(installation.CommandSetDirectory, year.ToString()));

        File.Copy(
            Path.Combine(TestFactory.RepositoryRoot(), "plugin", RevitInstallations.AddinFileName),
            installation.AddinManifestPath,
            overwrite: true);
        File.WriteAllText(Path.Combine(installation.PluginDirectory, "RevitMCPPlugin.dll"), string.Empty);
        File.WriteAllText(
            Path.Combine(installation.CommandSetDirectory, year.ToString(), "RevitMCPCommandSet.dll"),
            string.Empty);

        // The real manifest, so the generated registry is checked against the shipping command list.
        var manifest = Path.Combine(TestFactory.RepositoryRoot(), "command.json");
        File.Copy(manifest, Path.Combine(installation.CommandSetDirectory, "command.json"), overwrite: true);
        ManifestCommandCount = JsonNode.Parse(File.ReadAllText(manifest))!["commands"]!.AsArray().Count;
    }

    public RevitInstallation Installation(int year) => new()
    {
        Year = year,
        AddinsDirectory = Path.Combine(Paths.RevitAddinsRoot, year.ToString()),
        RevitInstalled = true,
        PluginInstalled = true
    };

    public JsonObject CommandRegistry(int year) =>
        JsonNode.Parse(File.ReadAllText(Installation(year).CommandRegistryPath))!.AsObject();

    public JsonObject ClaudeDesktopConfig() =>
        JsonNode.Parse(File.ReadAllText(Paths.ClaudeDesktopConfigFile))!.AsObject();

    public void WriteClaudeDesktopConfig(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Paths.ClaudeDesktopConfigFile)!);
        File.WriteAllText(Paths.ClaudeDesktopConfigFile, json);
    }

    public int Run(params string[] verbAndOptions) => Run(new RecordingProcessRunner(), out _, verbAndOptions);

    public int Run(string verb, out string output) => Run(new RecordingProcessRunner(), out output, verb);

    public int Run(IProcessRunner runner, params string[] verbAndOptions) => Run(runner, out _, verbAndOptions);

    public int Run(IProcessRunner runner, string verb, out string output) => Run(runner, out output, verb);

    public int Run(IProcessRunner runner, out string output, params string[] verbAndOptions) =>
        RunRaw([.. verbAndOptions, .. DefaultOptions()], out output, runner);

    public int RunRaw(string[] args, out string output, IProcessRunner? runner = null)
    {
        var writer = new StringWriter();
        var exitCode = CliDispatcher.Run(args, writer, runner ?? new RecordingProcessRunner());
        output = writer.ToString();
        return exitCode;
    }

    private string[] DefaultOptions() =>
    [
        "--addins-root", Paths.RevitAddinsRoot,
        "--autodesk-root", Paths.AutodeskRoot,
        "--claude-desktop-config", Paths.ClaudeDesktopConfigFile,
        "--server-path", ServerPath
    ];

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }
}

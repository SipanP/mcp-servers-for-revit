using System.Text.Json;
using System.Text.Json.Nodes;

namespace RevitMcpServer.Setup;

/// <summary>
/// Writes <c>commandRegistry.json</c> with every command enabled.
/// </summary>
/// <remarks>
/// The plugin creates this file empty on first run, and the Settings dialog adds commands to it
/// only as the user ticks them. Until that happens nothing works, which is a step that reliably
/// defeats people installing by hand. Setup writes the same file the dialog would have written
/// with everything ticked, so the ribbon visit is optional rather than required.
/// </remarks>
public static class CommandRegistryWriter
{
    /// <summary>
    /// Builds the registry for one Revit installation from the command set's own
    /// <c>command.json</c> manifest.
    /// </summary>
    /// <returns>The number of commands enabled.</returns>
    public static int Write(RevitInstallation revit)
    {
        var manifestPath = Path.Combine(revit.CommandSetDirectory, "command.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                $"The command set manifest is missing at {manifestPath}. The plugin files for Revit {revit.Year} look incomplete.",
                manifestPath);
        }

        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject()
                       ?? throw new InvalidDataException($"{manifestPath} is not a JSON object.");

        var developer = manifest["developer"]?.DeepClone();
        var versions = SupportedVersions(revit);

        var commands = new JsonArray();
        foreach (var entry in manifest["commands"]?.AsArray() ?? [])
        {
            var commandName = entry?["commandName"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(commandName))
            {
                continue;
            }

            var assemblyFile = entry?["assemblyPath"]?.GetValue<string>() ?? $"{RevitInstallations.CommandSetName}.dll";

            commands.Add(new JsonObject
            {
                ["commandName"] = commandName,
                // Resolved by the plugin relative to its Commands directory, with {VERSION}
                // replaced by the running Revit version. Backslashes because Revit is Windows-only.
                ["assemblyPath"] = $@"{RevitInstallations.CommandSetName}\{{VERSION}}\{assemblyFile}",
                ["enabled"] = true,
                ["description"] = entry?["description"]?.GetValue<string>() ?? string.Empty,
                ["supportedRevitVersions"] = new JsonArray([.. versions.Select(v => JsonValue.Create(v))]),
                ["developer"] = developer?.DeepClone()
            });
        }

        var registry = new JsonObject
        {
            ["commands"] = commands,
            ["settings"] = new JsonObject
            {
                ["logLevel"] = "Info",
                ["port"] = 8080
            }
        };

        Directory.CreateDirectory(revit.CommandsDirectory);
        File.WriteAllText(
            revit.CommandRegistryPath,
            registry.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return commands.Count;
    }

    /// <summary>
    /// The version subfolders that actually contain the command set assembly, mirroring how the
    /// Settings dialog derives <c>supportedRevitVersions</c>.
    /// </summary>
    private static IReadOnlyList<string> SupportedVersions(RevitInstallation revit)
    {
        if (!Directory.Exists(revit.CommandSetDirectory))
        {
            return [revit.Year.ToString()];
        }

        var versions = Directory.GetDirectories(revit.CommandSetDirectory)
            .Select(directory => Path.GetFileName(directory) ?? string.Empty)
            .Where(name => int.TryParse(name, out _))
            .Order(StringComparer.Ordinal)
            .ToList();

        return versions.Count > 0 ? versions : [revit.Year.ToString()];
    }
}

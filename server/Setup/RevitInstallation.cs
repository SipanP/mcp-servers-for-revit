namespace RevitMcpServer.Setup;

/// <summary>One Revit version found on this machine, or one the plugin is already installed for.</summary>
public sealed record RevitInstallation
{
    public required int Year { get; init; }

    /// <summary>The add-ins directory for this version, whether or not it exists yet.</summary>
    public required string AddinsDirectory { get; init; }

    /// <summary><c>Revit.exe</c> was found for this version.</summary>
    public required bool RevitInstalled { get; init; }

    /// <summary>The plugin's add-in manifest is already present for this version.</summary>
    public required bool PluginInstalled { get; init; }

    public string AddinManifestPath => Path.Combine(AddinsDirectory, RevitInstallations.AddinFileName);

    public string PluginDirectory => Path.Combine(AddinsDirectory, RevitInstallations.PluginFolderName);

    public string CommandsDirectory => Path.Combine(PluginDirectory, "Commands");

    public string CommandSetDirectory => Path.Combine(CommandsDirectory, RevitInstallations.CommandSetName);

    public string CommandRegistryPath => Path.Combine(CommandsDirectory, "commandRegistry.json");
}

/// <summary>Finds the Revit versions worth setting up on this machine.</summary>
public static class RevitInstallations
{
    public const string AddinFileName = "mcp-servers-for-revit.addin";
    public const string PluginFolderName = "revit_mcp_plugin";
    public const string CommandSetName = "RevitMCPCommandSet";

    /// <summary>The Revit versions this project builds for.</summary>
    public static readonly IReadOnlyList<int> SupportedYears = [2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027];

    /// <summary>
    /// Reports every supported version, flagging which have Revit installed and which already
    /// carry the plugin. A version counts as present if either is true: someone may have copied
    /// the add-in in by hand, or have Revit installed somewhere non-standard.
    /// </summary>
    public static IReadOnlyList<RevitInstallation> Detect(InstallPaths paths)
    {
        var found = new List<RevitInstallation>();

        foreach (var year in SupportedYears)
        {
            var addinsDirectory = Path.Combine(paths.RevitAddinsRoot, year.ToString());
            var revitInstalled = File.Exists(Path.Combine(paths.AutodeskRoot, $"Revit {year}", "Revit.exe"));
            var pluginInstalled = File.Exists(Path.Combine(addinsDirectory, AddinFileName));

            if (revitInstalled || pluginInstalled)
            {
                found.Add(new RevitInstallation
                {
                    Year = year,
                    AddinsDirectory = addinsDirectory,
                    RevitInstalled = revitInstalled,
                    PluginInstalled = pluginInstalled
                });
            }
        }

        return found;
    }
}

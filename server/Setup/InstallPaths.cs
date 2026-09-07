namespace RevitMcpServer.Setup;

/// <summary>
/// The filesystem locations setup and diagnostics work against. Injectable so the whole
/// installation flow can be exercised against temporary directories in tests.
/// </summary>
public sealed class InstallPaths
{
    /// <summary>Per-user Revit add-ins root, e.g. <c>%AppData%\Autodesk\Revit\Addins</c>.</summary>
    public required string RevitAddinsRoot { get; init; }

    /// <summary>Where Revit itself is installed, e.g. <c>%ProgramFiles%\Autodesk</c>.</summary>
    public required string AutodeskRoot { get; init; }

    /// <summary>Claude Desktop's config file, e.g. <c>%AppData%\Claude\claude_desktop_config.json</c>.</summary>
    public required string ClaudeDesktopConfigFile { get; init; }

    /// <summary>
    /// Per-user install locations, so setup never needs administrator rights — one less
    /// prompt for someone who is not comfortable with them.
    /// </summary>
    public static InstallPaths ForCurrentUser()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return new InstallPaths
        {
            RevitAddinsRoot = Path.Combine(appData, "Autodesk", "Revit", "Addins"),
            AutodeskRoot = Path.Combine(programFiles, "Autodesk"),
            ClaudeDesktopConfigFile = Path.Combine(appData, "Claude", "claude_desktop_config.json")
        };
    }
}

namespace RevitMcpServer.Setup;

/// <summary>Parsed command line for the setup-related verbs.</summary>
public sealed class SetupOptions
{
    /// <summary>Revit versions to act on. Empty means every version detected.</summary>
    public IReadOnlyList<int> Years { get; init; } = [];

    /// <summary>Absolute path to this server executable, as written into client configuration.</summary>
    public required string ServerExecutablePath { get; init; }

    public required InstallPaths Paths { get; init; }

    public bool ConfigureClaudeDesktop { get; init; } = true;

    public bool ConfigureClaudeCode { get; init; } = true;

    /// <summary>Parses arguments after the verb. Throws <see cref="ArgumentException"/> on bad input.</summary>
    public static SetupOptions Parse(IReadOnlyList<string> arguments)
    {
        var paths = InstallPaths.ForCurrentUser();
        var addinsRoot = paths.RevitAddinsRoot;
        var autodeskRoot = paths.AutodeskRoot;
        var claudeDesktopConfig = paths.ClaudeDesktopConfigFile;
        var serverPath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
        var years = new List<int>();
        var desktop = true;
        var code = true;

        for (var i = 0; i < arguments.Count; i++)
        {
            switch (arguments[i])
            {
                case "--revit":
                    foreach (var value in Next(arguments, ref i, "--revit").Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!int.TryParse(value.Trim(), out var year))
                        {
                            throw new ArgumentException($"'{value.Trim()}' is not a Revit version year.");
                        }

                        years.Add(year);
                    }

                    break;

                case "--server-path":
                    serverPath = Next(arguments, ref i, "--server-path");
                    break;

                case "--addins-root":
                    addinsRoot = Next(arguments, ref i, "--addins-root");
                    break;

                case "--autodesk-root":
                    autodeskRoot = Next(arguments, ref i, "--autodesk-root");
                    break;

                case "--claude-desktop-config":
                    claudeDesktopConfig = Next(arguments, ref i, "--claude-desktop-config");
                    break;

                case "--skip-claude-desktop":
                    desktop = false;
                    break;

                case "--skip-claude-code":
                    code = false;
                    break;

                default:
                    throw new ArgumentException($"Unknown option '{arguments[i]}'.");
            }
        }

        return new SetupOptions
        {
            Years = years,
            ServerExecutablePath = Path.GetFullPath(serverPath),
            ConfigureClaudeDesktop = desktop,
            ConfigureClaudeCode = code,
            Paths = new InstallPaths
            {
                RevitAddinsRoot = addinsRoot,
                AutodeskRoot = autodeskRoot,
                ClaudeDesktopConfigFile = claudeDesktopConfig
            }
        };
    }

    private static string Next(IReadOnlyList<string> arguments, ref int index, string option)
    {
        if (index + 1 >= arguments.Count)
        {
            throw new ArgumentException($"{option} needs a value.");
        }

        return arguments[++index];
    }

    /// <summary>The detected installations this run should act on.</summary>
    public IReadOnlyList<RevitInstallation> SelectInstallations()
    {
        var detected = RevitInstallations.Detect(Paths);
        return Years.Count == 0 ? detected : [.. detected.Where(install => Years.Contains(install.Year))];
    }
}

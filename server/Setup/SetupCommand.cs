namespace RevitMcpServer.Setup;

/// <summary>
/// Finishes an installation: enables the Revit commands and points the AI clients at this server.
/// File copying is the installer's job; everything that needs JSON edited correctly happens here.
/// </summary>
public static class SetupCommand
{
    public static int Run(SetupOptions options, IProcessRunner runner, TextWriter output)
    {
        var installations = options.SelectInstallations();
        var problems = 0;

        output.WriteLine("Setting up mcp-servers-for-revit");
        output.WriteLine($"  Server: {options.ServerExecutablePath}");
        output.WriteLine();

        if (!File.Exists(options.ServerExecutablePath))
        {
            output.WriteLine($"  ! The server executable was not found at {options.ServerExecutablePath}");
            problems++;
        }

        if (installations.Count == 0)
        {
            output.WriteLine("  ! No Revit installation was found.");
            output.WriteLine($"    Looked for Revit {RevitInstallations.SupportedYears[0]}-{RevitInstallations.SupportedYears[^1]} under {options.Paths.AutodeskRoot}");
            output.WriteLine("    and for existing add-ins under " + options.Paths.RevitAddinsRoot);
            problems++;
        }

        foreach (var revit in installations)
        {
            output.WriteLine($"  Revit {revit.Year}");

            if (!Directory.Exists(revit.PluginDirectory))
            {
                output.WriteLine($"    ! Plugin files are missing at {revit.PluginDirectory}");
                problems++;
                continue;
            }

            try
            {
                var enabled = CommandRegistryWriter.Write(revit);
                output.WriteLine($"    Enabled {enabled} commands");
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                output.WriteLine($"    ! Could not write the command registry: {ex.Message}");
                problems++;
            }
        }

        output.WriteLine();

        if (options.ConfigureClaudeDesktop)
        {
            problems += ConfigureClaudeDesktop(options, output);
        }

        if (options.ConfigureClaudeCode)
        {
            ConfigureClaudeCode(options, runner, output);
        }

        output.WriteLine();
        output.WriteLine(problems == 0 ? "Setup complete." : $"Setup finished with {problems} problem(s).");
        output.WriteLine();
        output.WriteLine("Next steps:");
        output.WriteLine("  1. Start Revit. If it asks about an add-in it does not recognise, choose 'Always Load'.");
        output.WriteLine("  2. Restart Claude Desktop so it picks up the new configuration.");
        output.WriteLine("  3. Run 'mcp-server-for-revit doctor' at any time to check the installation.");

        return problems == 0 ? 0 : 1;
    }

    private static int ConfigureClaudeDesktop(SetupOptions options, TextWriter output)
    {
        try
        {
            var result = ClaudeDesktopConfigurator.Configure(
                options.Paths.ClaudeDesktopConfigFile,
                options.ServerExecutablePath);

            output.WriteLine(result.Outcome switch
            {
                ClaudeDesktopOutcome.Created => $"  Claude Desktop: created {result.ConfigPath}",
                ClaudeDesktopOutcome.Added => "  Claude Desktop: added the server to your existing configuration",
                ClaudeDesktopOutcome.Updated => "  Claude Desktop: updated the existing entry",
                ClaudeDesktopOutcome.AlreadyCorrect => "  Claude Desktop: already configured",
                ClaudeDesktopOutcome.ReplacedUnreadableFile =>
                    $"  Claude Desktop: the configuration file could not be read, so it was rebuilt (previous file kept at {result.BackupPath})",
                _ => "  Claude Desktop: configured"
            });

            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            output.WriteLine($"  ! Claude Desktop: could not write {options.Paths.ClaudeDesktopConfigFile}: {ex.Message}");
            return 1;
        }
    }

    private static void ConfigureClaudeCode(SetupOptions options, IProcessRunner runner, TextWriter output)
    {
        var result = ClaudeCodeConfigurator.Configure(runner, options.ServerExecutablePath);

        // Claude Code not being installed is normal, so it is reported but never counted a problem.
        output.WriteLine(result.Outcome switch
        {
            ClaudeCodeOutcome.Registered => "  Claude Code: registered",
            ClaudeCodeOutcome.CliNotInstalled => "  Claude Code: not installed, skipped",
            _ => $"  Claude Code: could not register automatically. Run this yourself:{Environment.NewLine}      {result.Command}"
        });
    }
}

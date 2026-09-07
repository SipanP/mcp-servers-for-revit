namespace RevitMcpServer.Setup;

/// <summary>
/// Routes the installation verbs. With no arguments the process is an MCP server speaking over
/// stdio, so these verbs are the only case where writing to stdout is correct.
/// </summary>
public static class CliDispatcher
{
    private static readonly string[] Verbs = ["setup", "doctor", "uninstall", "--help", "-h", "help"];

    public static bool IsVerb(string[] args) => args.Length > 0 && Verbs.Contains(args[0]);

    public static int Run(string[] args, TextWriter output, IProcessRunner? runner = null)
    {
        runner ??= new ProcessRunner();
        var verb = args[0];

        if (verb is "--help" or "-h" or "help")
        {
            WriteUsage(output);
            return 0;
        }

        SetupOptions options;
        try
        {
            options = SetupOptions.Parse(args[1..]);
        }
        catch (ArgumentException ex)
        {
            output.WriteLine(ex.Message);
            output.WriteLine();
            WriteUsage(output);
            return 2;
        }

        return verb switch
        {
            "setup" => SetupCommand.Run(options, runner, output),
            "doctor" => DoctorCommand.Run(options, output),
            "uninstall" => UninstallCommand.Run(options, runner, output),
            _ => Unknown(verb, output)
        };
    }

    private static int Unknown(string verb, TextWriter output)
    {
        output.WriteLine($"Unknown command '{verb}'.");
        WriteUsage(output);
        return 2;
    }

    private static void WriteUsage(TextWriter output)
    {
        output.WriteLine(
            """
            mcp-server-for-revit

            Run with no arguments to start the MCP server on stdio. That is how AI clients
            launch it, and is what the installer configures.

            Commands:
              setup       Enable the Revit commands and point your AI clients at this server
              doctor      Check the installation and report anything wrong
              uninstall   Remove this server from your AI clients' configuration

            Options for all commands:
              --revit <years>              Comma-separated Revit versions (default: all detected)
              --server-path <path>         Path recorded in client configuration (default: this executable)
              --skip-claude-desktop        Leave Claude Desktop's configuration alone
              --skip-claude-code           Leave Claude Code's configuration alone
              --addins-root <path>         Override the Revit add-ins directory
              --autodesk-root <path>       Override where Revit is installed
              --claude-desktop-config <path>  Override Claude Desktop's config file
            """);
    }
}

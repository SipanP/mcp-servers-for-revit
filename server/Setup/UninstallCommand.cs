namespace RevitMcpServer.Setup;

/// <summary>
/// Removes this server from the AI clients' configuration. The installer deletes the files;
/// leaving a dead <c>mcpServers</c> entry behind would make Claude Desktop fail confusingly.
/// </summary>
public static class UninstallCommand
{
    public static int Run(SetupOptions options, IProcessRunner runner, TextWriter output)
    {
        output.WriteLine("Removing mcp-servers-for-revit from your AI clients");

        if (options.ConfigureClaudeDesktop)
        {
            var removed = ClaudeDesktopConfigurator.Remove(options.Paths.ClaudeDesktopConfigFile);
            output.WriteLine(removed
                ? "  Claude Desktop: entry removed"
                : "  Claude Desktop: nothing to remove");
        }

        if (options.ConfigureClaudeCode)
        {
            var removed = ClaudeCodeConfigurator.Remove(runner);
            output.WriteLine(removed
                ? "  Claude Code: entry removed"
                : "  Claude Code: nothing to remove");
        }

        return 0;
    }
}

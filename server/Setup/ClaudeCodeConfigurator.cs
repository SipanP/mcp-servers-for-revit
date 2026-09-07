namespace RevitMcpServer.Setup;

public enum ClaudeCodeOutcome
{
    /// <summary>The Claude Code CLI registered the server.</summary>
    Registered,

    /// <summary>The <c>claude</c> CLI is not installed, so there was nothing to configure.</summary>
    CliNotInstalled,

    /// <summary>The CLI was found but rejected the command.</summary>
    Failed
}

public sealed record ClaudeCodeResult(ClaudeCodeOutcome Outcome, string Command, string Output);

/// <summary>
/// Registers the server with Claude Code through its own CLI, rather than writing its config file
/// directly — the CLI owns that format and can change it.
/// </summary>
public static class ClaudeCodeConfigurator
{
    public const string ServerName = "mcp-server-for-revit";

    public static ClaudeCodeResult Configure(IProcessRunner runner, string serverExecutablePath)
    {
        // Removing first makes this idempotent: `claude mcp add` refuses an existing name.
        runner.Run("claude", ["mcp", "remove", ServerName, "--scope", "user"]);

        string[] arguments = ["mcp", "add", ServerName, "--scope", "user", "--", serverExecutablePath];
        var result = runner.Run("claude", arguments);
        var command = $"claude {string.Join(' ', arguments)}";

        return result switch
        {
            { Started: false } => new ClaudeCodeResult(ClaudeCodeOutcome.CliNotInstalled, command, string.Empty),
            { Succeeded: true } => new ClaudeCodeResult(ClaudeCodeOutcome.Registered, command, result.Output),
            _ => new ClaudeCodeResult(ClaudeCodeOutcome.Failed, command, result.Output)
        };
    }

    public static bool Remove(IProcessRunner runner) =>
        runner.Run("claude", ["mcp", "remove", ServerName, "--scope", "user"]).Succeeded;
}

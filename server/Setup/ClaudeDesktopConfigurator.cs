using System.Text.Json;
using System.Text.Json.Nodes;

namespace RevitMcpServer.Setup;

/// <summary>What happened to Claude Desktop's configuration file.</summary>
public enum ClaudeDesktopOutcome
{
    /// <summary>The entry was added to an existing configuration.</summary>
    Added,

    /// <summary>An existing entry was updated to point at this server.</summary>
    Updated,

    /// <summary>The entry was already correct.</summary>
    AlreadyCorrect,

    /// <summary>No configuration file existed, so one was created.</summary>
    Created,

    /// <summary>The file could not be parsed; it was backed up and rewritten.</summary>
    ReplacedUnreadableFile
}

public sealed record ClaudeDesktopResult(ClaudeDesktopOutcome Outcome, string ConfigPath, string? BackupPath);

/// <summary>
/// Points Claude Desktop at this server by editing <c>claude_desktop_config.json</c>.
/// </summary>
/// <remarks>
/// Hand-editing this file is the step people get wrong: it lives in a hidden folder, may not exist
/// yet, and needs a Windows path with every backslash doubled. Doing it here means the path is
/// escaped correctly by the serializer and any other MCP servers already configured are preserved.
/// </remarks>
public static class ClaudeDesktopConfigurator
{
    public const string ServerKey = "mcp-server-for-revit";

    public static ClaudeDesktopResult Configure(string configPath, string serverExecutablePath)
    {
        var (root, outcome, backupPath) = LoadOrCreate(configPath);

        if (root["mcpServers"] is not JsonObject servers)
        {
            servers = [];
            root["mcpServers"] = servers;
        }

        if (outcome is null)
        {
            outcome = servers[ServerKey] switch
            {
                JsonObject existing when Matches(existing, serverExecutablePath) => ClaudeDesktopOutcome.AlreadyCorrect,
                not null => ClaudeDesktopOutcome.Updated,
                _ => ClaudeDesktopOutcome.Added
            };
        }

        if (outcome is ClaudeDesktopOutcome.AlreadyCorrect)
        {
            return new ClaudeDesktopResult(outcome.Value, configPath, backupPath);
        }

        servers[ServerKey] = new JsonObject { ["command"] = serverExecutablePath };

        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(configPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return new ClaudeDesktopResult(outcome.Value, configPath, backupPath);
    }

    /// <summary>Removes this server's entry, leaving any others untouched.</summary>
    public static bool Remove(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return false;
        }

        JsonObject root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(configPath))?.AsObject() ?? [];
        }
        catch (JsonException)
        {
            return false;
        }

        if (root["mcpServers"] is not JsonObject servers || !servers.Remove(ServerKey))
        {
            return false;
        }

        Backup(configPath);
        File.WriteAllText(configPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return true;
    }

    private static (JsonObject Root, ClaudeDesktopOutcome? Outcome, string? BackupPath) LoadOrCreate(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return ([], ClaudeDesktopOutcome.Created, null);
        }

        var backupPath = Backup(configPath);

        try
        {
            var parsed = JsonNode.Parse(File.ReadAllText(configPath))?.AsObject();
            return parsed is null
                ? ([], ClaudeDesktopOutcome.ReplacedUnreadableFile, backupPath)
                : (parsed, null, backupPath);
        }
        catch (JsonException)
        {
            // Rewriting beats refusing to install, but the original is kept so nothing is lost.
            return ([], ClaudeDesktopOutcome.ReplacedUnreadableFile, backupPath);
        }
    }

    private static string Backup(string configPath)
    {
        var backupPath = configPath + ".backup";
        File.Copy(configPath, backupPath, overwrite: true);
        return backupPath;
    }

    private static bool Matches(JsonObject entry, string serverExecutablePath) =>
        entry["command"]?.GetValue<string>() == serverExecutablePath
        && entry["args"] is null or JsonArray { Count: 0 };
}

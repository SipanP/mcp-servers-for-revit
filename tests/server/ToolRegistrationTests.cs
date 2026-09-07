using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace RevitMcpServer.Tests;

/// <summary>
/// Guards the tool surface: the schemas clients see, and the tool-name to plugin-command mapping
/// that nothing else in the build checks.
/// </summary>
public class ToolRegistrationTests
{
    /// <summary>Tools that are answered locally from SQLite and never reach Revit.</summary>
    private static readonly string[] LocalOnlyTools =
        ["store_project_data", "store_room_data", "query_stored_data"];

    /// <summary>Tools whose MCP name differs from the command name the plugin registers.</summary>
    private static readonly Dictionary<string, string> CommandNameOverrides = new()
    {
        ["color_elements"] = "color_splash",
        ["tag_all_rooms"] = "tag_rooms",
        ["tag_all_walls"] = "tag_walls"
    };

    private static IReadOnlyList<McpServerTool> Tools()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(RevitMcpServer.Tools.SayHelloTool).Assembly);
        return [.. services.BuildServiceProvider().GetServices<McpServerTool>()];
    }

    [Test]
    public async Task Every_revit_tool_maps_onto_a_command_the_plugin_registers()
    {
        var declared = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(TestFactory.RepositoryRoot(), "command.json")))!
            ["commands"]!.AsArray()
            .Select(command => command!["commandName"]!.GetValue<string>())
            .ToHashSet(StringComparer.Ordinal);

        var missing = Tools()
            .Select(tool => tool.ProtocolTool.Name)
            .Where(name => !LocalOnlyTools.Contains(name))
            .Select(name => CommandNameOverrides.GetValueOrDefault(name, name))
            .Where(command => !declared.Contains(command))
            .ToList();

        await Assert.That(missing).IsEmpty();
    }

    /// <summary>
    /// The tool surface is this project's public contract. Set
    /// <c>REVIT_MCP_UPDATE_SNAPSHOTS=1</c> to rewrite the fixture after an intended change,
    /// then review the diff.
    /// </summary>
    [Test]
    public async Task Tool_names_and_schemas_match_the_recorded_snapshot()
    {
        var actual = Canonicalise(Snapshot());

        if (Environment.GetEnvironmentVariable("REVIT_MCP_UPDATE_SNAPSHOTS") == "1")
        {
            await File.WriteAllTextAsync(
                Path.Combine(TestFactory.RepositoryRoot(), "tests", "server", "Fixtures", "tool-schemas.json"),
                actual);
        }

        var expected = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "tool-schemas.json"));

        await Assert.That(actual).IsEqualTo(Canonicalise(JsonNode.Parse(expected)!));
    }

    [Test]
    public async Task Every_tool_carries_a_description()
    {
        var undocumented = Tools()
            .Where(tool => string.IsNullOrWhiteSpace(tool.ProtocolTool.Description))
            .Select(tool => tool.ProtocolTool.Name)
            .ToList();

        await Assert.That(undocumented).IsEmpty();
    }

    internal static JsonNode Snapshot()
    {
        var snapshot = new JsonObject();
        foreach (var tool in Tools().OrderBy(t => t.ProtocolTool.Name, StringComparer.Ordinal))
        {
            snapshot[tool.ProtocolTool.Name] = new JsonObject
            {
                ["description"] = tool.ProtocolTool.Description,
                ["inputSchema"] = JsonNode.Parse(tool.ProtocolTool.InputSchema.GetRawText())
            };
        }

        return snapshot;
    }

    /// <summary>Serialises with object keys sorted, so ordering never fails the comparison.</summary>
    private static string Canonicalise(JsonNode node) =>
        JsonSerializer.Serialize(Sort(node), new JsonSerializerOptions { WriteIndented = true });

    private static JsonNode? Sort(JsonNode? node) => node switch
    {
        JsonObject o => new JsonObject(o.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new KeyValuePair<string, JsonNode?>(p.Key, Sort(p.Value?.DeepClone())))),
        JsonArray a => new JsonArray([.. a.Select(item => Sort(item?.DeepClone()))]),
        _ => node?.DeepClone()
    };
}

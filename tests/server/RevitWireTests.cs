using System.Text.Json;
using RevitMcpServer.Tools;

namespace RevitMcpServer.Tests;

/// <summary>
/// Covers the contract with the Revit plugin: what goes on the wire, and how responses are framed
/// and surfaced. These are the behaviours a client would notice if the port drifted.
/// </summary>
public class RevitWireTests
{
    [Test]
    public async Task Sends_a_json_rpc_request_naming_the_tool_command()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"ok":true}""");

        await new SayHelloTool(TestFactory.ConnectionFor(revit)).SayHelloAsync("hi there");

        var request = revit.LastRequest;
        await Assert.That(request.GetProperty("jsonrpc").GetString()).IsEqualTo("2.0");
        await Assert.That(request.GetProperty("method").GetString()).IsEqualTo("say_hello");
        await Assert.That(request.GetProperty("id").GetString()).IsNotNullOrEmpty();
        await Assert.That(request.GetProperty("params").GetProperty("message").GetString()).IsEqualTo("hi there");
    }

    [Test]
    public async Task Renders_the_result_as_indented_json()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"greeting":"Hello MCP!"}""");

        var text = await new SayHelloTool(TestFactory.ConnectionFor(revit)).SayHelloAsync();

        await Assert.That(text).IsEqualTo("{\n  \"greeting\": \"Hello MCP!\"\n}");
    }

    [Test]
    public async Task Omits_unset_optional_parameters_so_the_payload_matches_the_previous_server()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"tagged":0}""");

        await new TagAllWallsTool(TestFactory.ConnectionFor(revit)).TagAllWallsAsync();

        var parameters = revit.LastRequest.GetProperty("params");
        await Assert.That(parameters.TryGetProperty("tagTypeId", out _)).IsFalse();
        await Assert.That(parameters.GetProperty("useLeader").GetBoolean()).IsFalse();
    }

    [Test]
    public async Task Maps_tool_names_onto_the_command_names_the_plugin_registers()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"tagged":0}""");

        await new TagAllRoomsTool(TestFactory.ConnectionFor(revit)).TagAllRoomsAsync();

        await Assert.That(revit.LastRequest.GetProperty("method").GetString()).IsEqualTo("tag_rooms");
    }

    [Test]
    public async Task Reassembles_a_response_delivered_across_several_writes()
    {
        // The plugin sends unframed JSON, so the client has to keep reading until it parses.
        await using var revit = FakeRevitServer.StartReturning("""{"elements":[1,2,3],"count":3}""", chunkSize: 7);

        var text = await new GetSelectedElementsTool(TestFactory.ConnectionFor(revit)).GetSelectedElementsAsync();

        await Assert.That(JsonDocument.Parse(text).RootElement.GetProperty("count").GetInt32()).IsEqualTo(3);
    }

    [Test]
    public async Task Surfaces_a_json_rpc_error_as_prefixed_tool_text()
    {
        await using var revit = FakeRevitServer.StartFailing("No active document");

        var text = await new SayHelloTool(TestFactory.ConnectionFor(revit)).SayHelloAsync();

        await Assert.That(text).IsEqualTo("Say hello failed: No active document");
    }

    [Test]
    public async Task Surfaces_a_connection_failure_as_prefixed_tool_text()
    {
        var text = await new SayHelloTool(TestFactory.UnreachableConnection()).SayHelloAsync();

        await Assert.That(text).StartsWith("Say hello failed: ");
        await Assert.That(text).Contains("Revit");
    }

    [Test]
    public async Task Applies_the_javascript_style_default_for_a_zero_limit()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"elements":[]}""");

        await new GetSelectedElementsTool(TestFactory.ConnectionFor(revit)).GetSelectedElementsAsync(limit: 0);

        await Assert.That(revit.LastRequest.GetProperty("params").GetProperty("limit").GetDouble()).IsEqualTo(100d);
    }

    [Test]
    public async Task Serialises_enums_using_the_literal_values_the_plugin_expects()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"grids":[]}""");

        await new CreateGridTool(TestFactory.ConnectionFor(revit))
            .CreateGridAsync(xCount: 2, xSpacing: 6000, yCount: 3, ySpacing: 6000);

        var parameters = revit.LastRequest.GetProperty("params");
        await Assert.That(parameters.GetProperty("xNamingStyle").GetString()).IsEqualTo("alphabetic");
        await Assert.That(parameters.GetProperty("yNamingStyle").GetString()).IsEqualTo("numeric");
    }

    [Test]
    public async Task Runs_commands_one_at_a_time()
    {
        await using var revit = FakeRevitServer.StartReturning("""{"ok":true}""");
        var connection = TestFactory.ConnectionFor(revit);
        var tool = new SayHelloTool(connection);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(i => tool.SayHelloAsync($"message {i}")));

        await Assert.That(revit.Requests.Count).IsEqualTo(5);
    }
}

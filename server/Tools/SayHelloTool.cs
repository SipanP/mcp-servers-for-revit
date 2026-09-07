using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class SayHelloTool(RevitConnection revit)
{
    [McpServerTool(Name = "say_hello")]
    [Description("Display a greeting dialog in Revit. Useful for testing the connection between Claude and Revit.")]
    public Task<string> SayHelloAsync(
        [Description("Optional custom message to display in the dialog. Defaults to 'Hello MCP!'")]
        string? message = null,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("say_hello", new { message }, "Say hello failed", cancellationToken);
}

using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class DeleteElementTool(RevitConnection revit)
{
    [McpServerTool(Name = "delete_element")]
    [Description("Delete one or more elements from the Revit model by their element IDs.")]
    public Task<string> DeleteElementAsync(
        [Description("The IDs of the elements to delete")] IReadOnlyList<string> elementIds,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("delete_element", new { elementIds }, "delete element failed", cancellationToken);
}

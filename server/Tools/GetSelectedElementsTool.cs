using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class GetSelectedElementsTool(RevitConnection revit)
{
    [McpServerTool(Name = "get_selected_elements")]
    [Description("Get elements currently selected in Revit. You can limit the number of returned elements.")]
    public Task<string> GetSelectedElementsAsync(
        [Description("Maximum number of elements to return")]
        double? limit = null,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "get_selected_elements",
            new { limit = JsDefaults.Or(limit, 100) },
            "get selected elements failed",
            cancellationToken);
}

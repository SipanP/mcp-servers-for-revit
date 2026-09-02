using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class GetCurrentViewInfoTool(RevitConnection revit)
{
    [McpServerTool(Name = "get_current_view_info")]
    [Description("Get detailed information about the currently active view in Revit, including its type, name and scale.")]
    public Task<string> GetCurrentViewInfoAsync(CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("get_current_view_info", new { }, "get current view info failed", cancellationToken);
}

using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class OperateElementTool(RevitConnection revit)
{
    [McpServerTool(Name = "operate_element")]
    [Description("Operate on Revit elements by performing actions such as select, selectionBox, setColor, setTransparency, delete, hide, etc.")]
    public Task<string> OperateElementAsync(
        [Description("Parameters for operating on Revit elements with specific actions")]
        OperateElementSpec data,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("operate_element", new { data }, "Operate elements failed", cancellationToken);
}

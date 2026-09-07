using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateLineBasedElementTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_line_based_element")]
    [Description("Create one or more line-based elements in Revit such as walls, beams, or pipes. Supports batch creation with detailed parameters including family type ID, start and end points, thickness, height, and level information. All units are in millimeters (mm).")]
    public Task<string> CreateLineBasedElementAsync(
        [Description("Array of line-based elements to create")] IReadOnlyList<LineBasedElementSpec> data,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("create_line_based_element", new { data }, "Create line-based element failed", cancellationToken);
}

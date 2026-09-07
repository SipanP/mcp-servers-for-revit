using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreatePointBasedElementTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_point_based_element")]
    [Description("Create one or more point-based elements in Revit such as doors, windows, or furniture. Supports batch creation with detailed parameters including family type ID, position, dimensions, and level information. All units are in millimeters (mm).")]
    public Task<string> CreatePointBasedElementAsync(
        [Description("Array of point-based elements to create")] IReadOnlyList<PointBasedElementSpec> data,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("create_point_based_element", new { data }, "Create point-based element failed", cancellationToken);
}

using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateSurfaceBasedElementTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_surface_based_element")]
    [Description("Create one or more surface-based elements in Revit such as floors, ceilings, or roofs. Supports batch creation with detailed parameters including family type ID, boundary lines, thickness, and level information. All units are in millimeters (mm).")]
    public Task<string> CreateSurfaceBasedElementAsync(
        [Description("Array of surface-based elements to create")] IReadOnlyList<SurfaceBasedElementSpec> data,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("create_surface_based_element", new { data }, "Create surface-based element failed", cancellationToken);
}

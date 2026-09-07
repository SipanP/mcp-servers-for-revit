using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateDimensionsTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_dimensions")]
    [Description("Create dimension annotations in the current Revit view. Supports dimensioning between elements (walls, doors, windows) by element IDs, or between two points with automatic reference detection. All coordinates are in millimeters (mm).")]
    public Task<string> CreateDimensionsAsync(
        [Description("Array of dimensions to create")] IReadOnlyList<DimensionSpec> dimensions,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("create_dimensions", new { dimensions }, "Dimension creation failed", cancellationToken);
}

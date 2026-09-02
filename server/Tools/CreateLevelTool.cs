using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateLevelTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_level")]
    [Description("Create one or more levels in Revit at specified elevations. Levels define horizontal planes in the building and are used to host floor plans, ceilings, and other level-based elements. All elevation units are in millimeters (mm).")]
    public Task<string> CreateLevelAsync(
        [Description("Array of levels to create")] IReadOnlyList<LevelSpec> data,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("create_level", new { data }, "Create level failed", cancellationToken);
}

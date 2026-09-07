using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class GetMaterialQuantitiesTool(RevitConnection revit)
{
    [McpServerTool(Name = "get_material_quantities")]
    [Description("Calculate material quantities and takeoffs from the current Revit project. Returns detailed information about each material including name, class, area, volume, and element counts. Useful for cost estimation, material ordering, and sustainability analysis.")]
    public Task<string> GetMaterialQuantitiesAsync(
        [Description("Optional list of Revit category names to filter by (e.g., ['OST_Walls', 'OST_Floors', 'OST_Roofs']). If not specified, all categories are included.")]
        IReadOnlyList<string>? categoryFilters = null,
        [Description("Whether to only analyze currently selected elements. Defaults to false (analyze entire project).")]
        bool selectedElementsOnly = false,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "get_material_quantities",
            new { categoryFilters, selectedElementsOnly },
            "Get material quantities failed",
            cancellationToken);
}

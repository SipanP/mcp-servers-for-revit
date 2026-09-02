using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class GetAvailableFamilyTypesTool(RevitConnection revit)
{
    [McpServerTool(Name = "get_available_family_types")]
    [Description("Get available family types in the current Revit project. You can filter by category and family name, and limit the number of returned types.")]
    public Task<string> GetAvailableFamilyTypesAsync(
        [Description("List of Revit category names to filter by (e.g., 'OST_Walls', 'OST_Doors', 'OST_Furniture')")]
        IReadOnlyList<string>? categoryList = null,
        [Description("Filter family types by family name (partial match)")]
        string? familyNameFilter = null,
        [Description("Maximum number of family types to return")]
        double? limit = null,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "get_available_family_types",
            new
            {
                categoryList = categoryList ?? [],
                familyNameFilter = familyNameFilter ?? string.Empty,
                limit = JsDefaults.Or(limit, 100)
            },
            "get available family types failed",
            cancellationToken);
}

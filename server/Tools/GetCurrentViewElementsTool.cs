using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class GetCurrentViewElementsTool(RevitConnection revit)
{
    [McpServerTool(Name = "get_current_view_elements")]
    [Description("Get elements from the current active view in Revit. You can filter by model categories (like Walls, Floors) or annotation categories (like Dimensions, Text). Use includeHidden to show/hide invisible elements and limit to control the number of returned elements.")]
    public Task<string> GetCurrentViewElementsAsync(
        [Description("List of Revit model category names (e.g., 'OST_Walls', 'OST_Doors', 'OST_Floors')")]
        IReadOnlyList<string>? modelCategoryList = null,
        [Description("List of Revit annotation category names (e.g., 'OST_Dimensions', 'OST_WallTags', 'OST_TextNotes')")]
        IReadOnlyList<string>? annotationCategoryList = null,
        [Description("Whether to include hidden elements in the results")]
        bool? includeHidden = null,
        [Description("Maximum number of elements to return")]
        double? limit = null,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "get_current_view_elements",
            new
            {
                modelCategoryList = modelCategoryList ?? [],
                annotationCategoryList = annotationCategoryList ?? [],
                includeHidden = includeHidden ?? false,
                limit = JsDefaults.Or(limit, 100)
            },
            "get current view elements failed",
            cancellationToken);
}

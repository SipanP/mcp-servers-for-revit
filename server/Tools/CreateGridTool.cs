using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateGridTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_grid")]
    [Description("Create a grid system in Revit with smart spacing generation. Supports both X-axis (vertical) and Y-axis (horizontal) grids with customizable naming styles (alphabetic A,B,C or numeric 1,2,3). All units are in millimeters (mm).")]
    public Task<string> CreateGridAsync(
        [Description("Number of grid lines along X-axis (vertical grids)")] [Range(1, int.MaxValue)] int xCount,
        [Description("Spacing between X-axis grid lines in millimeters")] double xSpacing,
        [Description("Number of grid lines along Y-axis (horizontal grids)")] [Range(1, int.MaxValue)] int yCount,
        [Description("Spacing between Y-axis grid lines in millimeters")] double ySpacing,
        [Description("Starting label for X-axis grids (e.g., 'A' or '1')")]
        string xStartLabel = "A",
        [Description("Naming style for X-axis: 'alphabetic' (A,B,C...) or 'numeric' (1,2,3...)")]
        GridNamingStyle xNamingStyle = GridNamingStyle.alphabetic,
        [Description("Starting label for Y-axis grids (e.g., '1' or 'A')")]
        string yStartLabel = "1",
        [Description("Naming style for Y-axis: 'alphabetic' (A,B,C...) or 'numeric' (1,2,3...)")]
        GridNamingStyle yNamingStyle = GridNamingStyle.numeric,
        [Description("Minimum extent along X-axis in mm (where Y-axis grids start)")]
        double xExtentMin = 0,
        [Description("Maximum extent along X-axis in mm (where Y-axis grids end)")]
        double xExtentMax = 50000,
        [Description("Minimum extent along Y-axis in mm (where X-axis grids start)")]
        double yExtentMin = 0,
        [Description("Maximum extent along Y-axis in mm (where X-axis grids end)")]
        double yExtentMax = 50000,
        [Description("Elevation for grid lines in mm (Z-coordinate)")]
        double elevation = 0,
        [Description("Starting position for first X-axis grid in mm")]
        double xStartPosition = 0,
        [Description("Starting position for first Y-axis grid in mm")]
        double yStartPosition = 0,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "create_grid",
            new
            {
                xCount,
                xSpacing,
                xStartLabel,
                xNamingStyle,
                yCount,
                ySpacing,
                yStartLabel,
                yNamingStyle,
                xExtentMin,
                xExtentMax,
                yExtentMin,
                yExtentMax,
                elevation,
                xStartPosition,
                yStartPosition
            },
            "Create grid failed",
            cancellationToken);
}

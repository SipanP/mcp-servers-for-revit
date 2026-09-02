using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateStructuralFramingSystemTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_structural_framing_system")]
    [Description("Create a structural beam framing system in Revit. Generates beams within a rectangular boundary at fixed spacing intervals. The system uses Revit's BeamSystem API to create properly connected beam layouts. All units are in millimeters (mm).")]
    public Task<string> CreateStructuralFramingSystemAsync(
        [Description("Name of the level to place the beam system on (e.g., 'Level 1', 'Level 2'). If the level doesn't exist but follows 'Level N' pattern, it will be auto-created at 4000mm floor-to-floor height.")]
        string levelName,
        [Description("Minimum X coordinate of the rectangular boundary in millimeters")] double xMin,
        [Description("Maximum X coordinate of the rectangular boundary in millimeters")] double xMax,
        [Description("Minimum Y coordinate of the rectangular boundary in millimeters")] double yMin,
        [Description("Maximum Y coordinate of the rectangular boundary in millimeters")] double yMax,
        [Description("Spacing between beams in millimeters")] double spacing,
        [Description("Which edge defines the beam direction. Beams run perpendicular to this edge. 'bottom'/'top' = beams run in Y direction, 'left'/'right' = beams run in X direction.")]
        BeamDirectionEdge directionEdge = BeamDirectionEdge.bottom,
        [Description("Layout rule type. Currently only 'fixed_distance' is supported.")]
        BeamLayoutRule layoutRule = BeamLayoutRule.fixed_distance,
        [Description("Beam justification within the layout. 'center' places beams symmetrically, 'beginning'/'end' align to boundary edges.")]
        BeamJustification justify = BeamJustification.center,
        [Description("Name of the beam family type to use (e.g., 'W10x12', 'W-Wide Flange'). If not provided, the first available structural framing type will be used.")]
        string? beamTypeName = null,
        [Description("Elevation offset from the level in millimeters. Use this to adjust the vertical position of the beam system.")]
        double elevation = 0,
        [Description("Whether to create a 3D beam system. Set to true for sloped or non-planar systems.")]
        bool is3d = false,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "create_structural_framing_system",
            new
            {
                levelName,
                xMin,
                xMax,
                yMin,
                yMax,
                spacing,
                directionEdge,
                layoutRule,
                justify,
                beamTypeName,
                elevation,
                is3d
            },
            "Create structural framing system failed",
            cancellationToken);
}

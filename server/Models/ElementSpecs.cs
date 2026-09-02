using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>A door, window, piece of furniture or other point-hosted element.</summary>
public sealed record PointBasedElementSpec
{
    [JsonPropertyName("name")]
    [Description("Description of the element (e.g., door, window)")]
    public required string Name { get; init; }

    [JsonPropertyName("typeId")]
    [Description("The ID of the family type to create.")]
    public double? TypeId { get; init; }

    [JsonPropertyName("locationPoint")]
    [Description("The position coordinates where the element will be placed")]
    public required ElementPoint LocationPoint { get; init; }

    [JsonPropertyName("width")]
    [Description("Width of the element in mm")]
    public required double Width { get; init; }

    [JsonPropertyName("depth")]
    [Description("Depth of the element in mm")]
    public double? Depth { get; init; }

    [JsonPropertyName("height")]
    [Description("Height of the element in mm")]
    public required double Height { get; init; }

    [JsonPropertyName("baseLevel")]
    [Description("Base level height")]
    public required double BaseLevel { get; init; }

    [JsonPropertyName("baseOffset")]
    [Description("Offset from the base level")]
    public required double BaseOffset { get; init; }

    [JsonPropertyName("rotation")]
    [Description("Rotation angle in degrees (0-360)")]
    public double? Rotation { get; init; }

    [JsonPropertyName("hostWallId")]
    [Description("The ElementId of a specific wall to use as host for doors/windows. If not provided, the nearest wall will be auto-detected.")]
    public double? HostWallId { get; init; }

    [JsonPropertyName("facingFlipped")]
    [Description("Whether to flip the facing direction of the door/window. When true, the element faces the opposite side of the wall.")]
    [DefaultValue(false)]
    public bool FacingFlipped { get; init; }
}

/// <summary>A wall, beam, pipe or other element driven by a location line.</summary>
public sealed record LineBasedElementSpec
{
    [JsonPropertyName("category")]
    [Description("Revit built-in category (e.g., OST_Walls, OST_StructuralFraming, OST_DuctCurves)")]
    public required string Category { get; init; }

    [JsonPropertyName("typeId")]
    [Description("The ID of the family type to create.")]
    public double? TypeId { get; init; }

    [JsonPropertyName("locationLine")]
    [Description("The line defining the element's location")]
    public required LineSpec LocationLine { get; init; }

    [JsonPropertyName("thickness")]
    [Description("Thickness/width of the element (e.g., wall thickness)")]
    public required double Thickness { get; init; }

    [JsonPropertyName("height")]
    [Description("Height of the element (e.g., wall height)")]
    public required double Height { get; init; }

    [JsonPropertyName("baseLevel")]
    [Description("Base level height")]
    public required double BaseLevel { get; init; }

    [JsonPropertyName("baseOffset")]
    [Description("Offset from the base level")]
    public required double BaseOffset { get; init; }
}

/// <summary>The closed outer loop bounding a surface-based element.</summary>
public sealed record SurfaceBoundarySpec
{
    [JsonPropertyName("outerLoop")]
    [Description("Array of line segments defining the boundary")]
    [MinLength(3)]
    public required IReadOnlyList<LineSpec> OuterLoop { get; init; }
}

/// <summary>A floor, ceiling or roof.</summary>
public sealed record SurfaceBasedElementSpec
{
    [JsonPropertyName("name")]
    [Description("Description of the element (e.g., floor, ceiling)")]
    public required string Name { get; init; }

    [JsonPropertyName("category")]
    [Description("The Revit built-in category for the element. Use OST_Floors for floors, OST_Ceilings for ceilings, OST_Roofs for roofs. If not specified, will be determined from typeId.")]
    public SurfaceCategory? Category { get; init; }

    [JsonPropertyName("typeId")]
    [Description("The ID of the family type to create.")]
    public double? TypeId { get; init; }

    [JsonPropertyName("boundary")]
    [Description("Boundary definition with outer loop")]
    public required SurfaceBoundarySpec Boundary { get; init; }

    [JsonPropertyName("thickness")]
    [Description("Thickness of the element")]
    public required double Thickness { get; init; }

    [JsonPropertyName("baseLevel")]
    [Description("Base level height")]
    public required double BaseLevel { get; init; }

    [JsonPropertyName("baseOffset")]
    [Description("Offset from the base level")]
    public required double BaseOffset { get; init; }
}

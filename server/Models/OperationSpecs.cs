using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>An explicit RGB colour.</summary>
public sealed record RgbColorSpec
{
    [JsonPropertyName("r")]
    [Range(0, 255)]
    public required int R { get; init; }

    [JsonPropertyName("g")]
    [Range(0, 255)]
    public required int G { get; init; }

    [JsonPropertyName("b")]
    [Range(0, 255)]
    public required int B { get; init; }
}

/// <summary>Parameters for <c>operate_element</c>.</summary>
public sealed record OperateElementSpec
{
    [JsonPropertyName("elementIds")]
    [Description("Array of Revit element IDs to perform the specified action on")]
    public required IReadOnlyList<double> ElementIds { get; init; }

    [JsonPropertyName("action")]
    [Description("The operation to perform on elements. Valid values: Select, SelectionBox, SetColor, SetTransparency, Delete, Hide, TempHide, Isolate, Unhide, ResetIsolate, Highlight. Select enables direct element selection in the active view. SelectionBox allows selection of elements by drawing a rectangular window in the view. SetColor changes the color of elements (requires elementColor parameter). SetTransparency adjusts element transparency (requires transparencyValue parameter). Highlight is a convenience operation that sets elements to red color (internally calls SetColor with red). Delete permanently removes elements from the project. Hide makes elements invisible in the current view until explicitly shown. TempHide temporarily hides elements in the current view. Isolate displays only selected elements while hiding all others. Unhide reveals previously hidden elements. ResetIsolate restores normal visibility to the view.")]
    public required string Action { get; init; }

    [JsonPropertyName("transparencyValue")]
    [Description("Transparency value (0-100) for SetTransparency action. Higher values increase transparency.")]
    [DefaultValue(50d)]
    public double TransparencyValue { get; init; } = 50;

    [JsonPropertyName("colorValue")]
    [Description("RGB color values for SetColor action. Default is red [255,0,0].")]
    public IReadOnlyList<double> ColorValue { get; init; } = [255, 0, 0];
}

/// <summary>Parameters for <c>ai_element_filter</c>.</summary>
public sealed record AiElementFilterSpec
{
    [JsonPropertyName("filterCategory")]
    [Description("Enumeration of built-in element categories in Revit used for filtering and identifying specific element types (e.g., OST_Walls, OST_Floors, OST_GenericModel). Note that furniture elements may be classified as either OST_Furniture or OST_GenericModel categories, requiring flexible selection approaches")]
    public string? FilterCategory { get; init; }

    [JsonPropertyName("filterElementType")]
    [Description("The Revit element type name used for filtering specific elements by their class or type (e.g., 'Wall', 'Floor', 'Autodesk.Revit.DB.Wall'). Gets or sets the name of the Revit element type to be filtered.")]
    public string? FilterElementType { get; init; }

    [JsonPropertyName("filterFamilySymbolId")]
    [Description("The ElementId of a specific FamilySymbol (type) in Revit used for filtering elements by their type (e.g., '123456', '789012'). Gets or sets the ElementId of the FamilySymbol to be used as a filter criterion. Use '-1' if no specific FamilySymbol filtering is needed.")]
    public double? FilterFamilySymbolId { get; init; }

    [JsonPropertyName("includeTypes")]
    [Description("Determines whether to include element types (such as wall types, door types, etc.) in the selection results. When set to true, element types will be included; when false, they will be excluded.")]
    [DefaultValue(false)]
    public bool IncludeTypes { get; init; }

    [JsonPropertyName("includeInstances")]
    [Description("Determines whether to include element instances (such as placed walls, doors, etc.) in the selection results. When set to true, element instances will be included; when false, they will be excluded.")]
    [DefaultValue(true)]
    public bool IncludeInstances { get; init; } = true;

    [JsonPropertyName("filterVisibleInCurrentView")]
    [Description("Determines whether to only return elements that are visible in the current view. When set to true, only elements visible in the current view will be returned. Note: This filter only applies to element instances, not type elements.")]
    public bool? FilterVisibleInCurrentView { get; init; }

    [JsonPropertyName("boundingBoxMin")]
    [Description("The minimum point coordinates (in mm) for spatial bounding box filtering. When set along with boundingBoxMax, only elements that intersect with this bounding box will be returned. Set to null to disable this filter.")]
    public LineSpec? BoundingBoxMin { get; init; }

    [JsonPropertyName("boundingBoxMax")]
    [Description("The maximum point coordinates (in mm) for spatial bounding box filtering. When set along with boundingBoxMin, only elements that intersect with this bounding box will be returned. Set to null to disable this filter.")]
    public LineSpec? BoundingBoxMax { get; init; }

    [JsonPropertyName("maxElements")]
    [Description("The maximum number of elements to find in a single tool invocation. Default is 50. Values exceeding 50 are not recommended for performance reasons.")]
    public double? MaxElements { get; init; }
}

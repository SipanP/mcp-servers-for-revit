using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>One dimension annotation to create in a view.</summary>
public sealed record DimensionSpec
{
    [JsonPropertyName("startPoint")]
    [Description("Start point of the dimension line (mm)")]
    public required MillimetrePoint StartPoint { get; init; }

    [JsonPropertyName("endPoint")]
    [Description("End point of the dimension line (mm)")]
    public required MillimetrePoint EndPoint { get; init; }

    [JsonPropertyName("linePoint")]
    [Description("Location of the dimension line itself (mm). If not provided, defaults to midpoint offset by 1 foot")]
    public MillimetrePoint? LinePoint { get; init; }

    [JsonPropertyName("elementIds")]
    [Description("Element IDs to dimension between. If provided, references are extracted from these elements. If empty, references are auto-detected at start/end points")]
    public IReadOnlyList<double>? ElementIds { get; init; }

    [JsonPropertyName("dimensionType")]
    [Description("Dimension type (default: 'Linear')")]
    [DefaultValue("Linear")]
    public string DimensionType { get; init; } = "Linear";

    [JsonPropertyName("dimensionStyleId")]
    [Description("Element ID of the dimension style to apply. -1 for default style")]
    [DefaultValue(-1d)]
    public double DimensionStyleId { get; init; } = -1;

    [JsonPropertyName("viewId")]
    [Description("Element ID of the view to create the dimension in. -1 for active view")]
    [DefaultValue(-1d)]
    public double ViewId { get; init; } = -1;
}

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>A point in project coordinates, in millimetres.</summary>
public sealed record MillimetrePoint
{
    [JsonPropertyName("x")]
    [Description("X coordinate in mm")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    [Description("Y coordinate in mm")]
    public required double Y { get; init; }

    [JsonPropertyName("z")]
    [Description("Z coordinate in mm")]
    public required double Z { get; init; }
}

/// <summary>A point that must fall inside an enclosed set of walls.</summary>
public sealed record RoomLocationPoint
{
    [JsonPropertyName("x")]
    [Description("X coordinate in mm (should be inside enclosed walls)")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    [Description("Y coordinate in mm (should be inside enclosed walls)")]
    public required double Y { get; init; }

    [JsonPropertyName("z")]
    [Description("Z coordinate in mm (typically 0 or level elevation)")]
    public required double Z { get; init; }
}

/// <summary>The placement point of a point-based element.</summary>
public sealed record ElementPoint
{
    [JsonPropertyName("x")]
    [Description("X coordinate")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    [Description("Y coordinate")]
    public required double Y { get; init; }

    [JsonPropertyName("z")]
    [Description("Z coordinate")]
    public required double Z { get; init; }
}

/// <summary>The first point of a line segment.</summary>
public sealed record SegmentStart
{
    [JsonPropertyName("x")]
    [Description("X coordinate of start point")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    [Description("Y coordinate of start point")]
    public required double Y { get; init; }

    [JsonPropertyName("z")]
    [Description("Z coordinate of start point")]
    public required double Z { get; init; }
}

/// <summary>The second point of a line segment.</summary>
public sealed record SegmentEnd
{
    [JsonPropertyName("x")]
    [Description("X coordinate of end point")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    [Description("Y coordinate of end point")]
    public required double Y { get; init; }

    [JsonPropertyName("z")]
    [Description("Z coordinate of end point")]
    public required double Z { get; init; }
}

/// <summary>A straight segment defined by its two end points.</summary>
public sealed record LineSpec
{
    [JsonPropertyName("p0")]
    public required SegmentStart P0 { get; init; }

    [JsonPropertyName("p1")]
    public required SegmentEnd P1 { get; init; }
}

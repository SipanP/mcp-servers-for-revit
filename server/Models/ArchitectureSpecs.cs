using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

/// <summary>One level to create, with its optional generated views.</summary>
public sealed record LevelSpec
{
    [JsonPropertyName("name")]
    [Description("Name of the level (e.g., 'Level 2', 'Roof', 'Basement')")]
    public required string Name { get; init; }

    [JsonPropertyName("elevation")]
    [Description("Elevation of the level in millimeters (mm) from project origin")]
    public required double Elevation { get; init; }

    [JsonPropertyName("description")]
    [Description("Optional description of the level")]
    public string? Description { get; init; }

    [JsonPropertyName("isMainLevel")]
    [Description("Whether this is a main level (default: true)")]
    [DefaultValue(true)]
    public bool IsMainLevel { get; init; } = true;

    [JsonPropertyName("isBuildingStory")]
    [Description("Whether this level represents a building story (default: true)")]
    [DefaultValue(true)]
    public bool IsBuildingStory { get; init; } = true;

    [JsonPropertyName("computationHeight")]
    [Description("Optional computation height in mm")]
    public double? ComputationHeight { get; init; }

    [JsonPropertyName("viewPlanOffset")]
    [Description("Optional view plan offset in mm")]
    public double? ViewPlanOffset { get; init; }

    [JsonPropertyName("viewSectionOffset")]
    [Description("Optional view section offset in mm")]
    public double? ViewSectionOffset { get; init; }

    [JsonPropertyName("viewElevationOffset")]
    [Description("Optional view elevation offset in mm")]
    public double? ViewElevationOffset { get; init; }

    [JsonPropertyName("createFloorPlan")]
    [Description("Whether to create a floor plan view for this level (default: true)")]
    [DefaultValue(true)]
    public bool CreateFloorPlan { get; init; } = true;

    [JsonPropertyName("createCeilingPlan")]
    [Description("Whether to create a ceiling plan view for this level (default: true)")]
    [DefaultValue(true)]
    public bool CreateCeilingPlan { get; init; } = true;
}

/// <summary>One room to place inside an enclosed wall boundary.</summary>
public sealed record RoomSpec
{
    [JsonPropertyName("name")]
    [Description("Room name (e.g., 'Server Room', 'Kitchen', 'Office')")]
    public required string Name { get; init; }

    [JsonPropertyName("number")]
    [Description("Room number (e.g., '101', 'A-01')")]
    public string? Number { get; init; }

    [JsonPropertyName("location")]
    [Description("The location point where the room will be placed - must be inside an enclosed area")]
    public required RoomLocationPoint Location { get; init; }

    [JsonPropertyName("levelId")]
    [Description("Revit Level ElementId. If not provided, uses the nearest level to the Z coordinate")]
    public double? LevelId { get; init; }

    [JsonPropertyName("upperLimitId")]
    [Description("Upper limit Level ElementId for room height")]
    public double? UpperLimitId { get; init; }

    [JsonPropertyName("limitOffset")]
    [Description("Offset from upper limit in mm")]
    public double? LimitOffset { get; init; }

    [JsonPropertyName("baseOffset")]
    [Description("Offset from base level in mm")]
    public double? BaseOffset { get; init; }

    [JsonPropertyName("department")]
    [Description("Department the room belongs to")]
    public string? Department { get; init; }

    [JsonPropertyName("comments")]
    [Description("Additional comments for the room")]
    public string? Comments { get; init; }
}

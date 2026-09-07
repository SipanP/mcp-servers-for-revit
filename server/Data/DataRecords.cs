using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Data;

/// <summary>Project metadata accepted by <c>store_project_data</c>.</summary>
public sealed record ProjectRecord
{
    [JsonPropertyName("project_name")]
    public required string ProjectName { get; init; }

    [JsonPropertyName("project_path")]
    public string? ProjectPath { get; init; }

    [JsonPropertyName("project_number")]
    public string? ProjectNumber { get; init; }

    [JsonPropertyName("project_address")]
    public string? ProjectAddress { get; init; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    [JsonPropertyName("project_status")]
    public string? ProjectStatus { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>One room accepted by <c>store_room_data</c>.</summary>
public sealed record RoomRecord
{
    [JsonPropertyName("room_id")]
    [Description("Unique identifier for the room (Revit Element ID)")]
    public required string RoomId { get; init; }

    [JsonPropertyName("room_name")]
    [Description("Room name")]
    public string? RoomName { get; init; }

    [JsonPropertyName("room_number")]
    [Description("Room number")]
    public string? RoomNumber { get; init; }

    [JsonPropertyName("department")]
    [Description("Department")]
    public string? Department { get; init; }

    [JsonPropertyName("level")]
    [Description("Level or floor")]
    public string? Level { get; init; }

    [JsonPropertyName("area")]
    [Description("Room area")]
    public double? Area { get; init; }

    [JsonPropertyName("perimeter")]
    [Description("Room perimeter")]
    public double? Perimeter { get; init; }

    [JsonPropertyName("occupancy")]
    [Description("Occupancy type")]
    public string? Occupancy { get; init; }

    [JsonPropertyName("comments")]
    [Description("Additional comments")]
    public string? Comments { get; init; }

    [JsonPropertyName("metadata")]
    [Description("Additional room metadata as key-value pairs")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

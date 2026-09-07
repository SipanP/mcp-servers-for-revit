using System.Text.Json.Serialization;

namespace RevitMcpServer.Models;

// Enum member names are the literal values sent to Revit, so they are spelled exactly as the
// plugin expects rather than in C# casing conventions.

/// <summary>Grid label sequence.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GridNamingStyle>))]
public enum GridNamingStyle
{
    alphabetic,
    numeric
}

/// <summary>Which boundary edge defines the direction of a beam system.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BeamDirectionEdge>))]
public enum BeamDirectionEdge
{
    bottom,
    right,
    top,
    left
}

/// <summary>Beam system layout rule.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BeamLayoutRule>))]
public enum BeamLayoutRule
{
    fixed_distance
}

/// <summary>Beam justification within the layout.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BeamJustification>))]
public enum BeamJustification
{
    beginning,
    center,
    end,
    directionline
}

/// <summary>Revit categories that can host a surface-based element.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SurfaceCategory>))]
public enum SurfaceCategory
{
    OST_Floors,
    OST_Ceilings,
    OST_Roofs
}

/// <summary>How a dynamic code snippet interacts with Revit transactions.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TransactionMode>))]
public enum TransactionMode
{
    auto,
    none
}

/// <summary>Supported queries against the local database.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StoredDataQueryType>))]
public enum StoredDataQueryType
{
    all_projects,
    project_by_id,
    project_by_name,
    rooms_by_project_id,
    rooms_by_project_name,
    all_rooms,
    stats
}

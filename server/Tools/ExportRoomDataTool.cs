using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class ExportRoomDataTool(RevitConnection revit)
{
    [McpServerTool(Name = "export_room_data")]
    [Description("Export all room data from the current Revit project. Returns detailed information about each room including name, number, level, area, volume, perimeter, department, and more. Useful for generating room schedules, space analysis, and facility management data.")]
    public Task<string> ExportRoomDataAsync(
        [Description("Whether to include unplaced rooms (rooms not yet placed in the model). Defaults to false.")]
        bool includeUnplacedRooms = false,
        [Description("Whether to include rooms that are not fully enclosed. Defaults to false.")]
        bool includeNotEnclosedRooms = false,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "export_room_data",
            new { includeUnplacedRooms, includeNotEnclosedRooms },
            "Export room data failed",
            cancellationToken);
}

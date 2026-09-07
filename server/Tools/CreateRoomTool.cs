using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class CreateRoomTool(RevitConnection revit)
{
    [McpServerTool(Name = "create_room")]
    [Description("Create and place rooms in Revit at specified locations. Rooms are placed within enclosed wall boundaries and can be named and numbered. The location point should be inside an enclosed area bounded by walls. All coordinates are in millimeters (mm).")]
    public Task<string> CreateRoomAsync(
        [Description("Array of rooms to create")] IReadOnlyList<RoomSpec> data,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync("create_room", new { data }, "Create room failed", cancellationToken);
}

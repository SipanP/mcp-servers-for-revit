using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class TagAllRoomsTool(RevitConnection revit)
{
    [McpServerTool(Name = "tag_all_rooms")]
    [Description("Create tags for all rooms in the current active view. Tags will be placed at the center point of each room, displaying the room name and number.")]
    public Task<string> TagAllRoomsAsync(
        [Description("Whether to use a leader line when creating the tags")]
        bool useLeader = false,
        [Description("The ID of the specific room tag family type to use. If not provided, the default room tag type will be used")]
        string? tagTypeId = null,
        [Description("Optional array of specific room element IDs to tag. If not provided, all rooms in the current view will be tagged")]
        IReadOnlyList<double>? roomIds = null,
        CancellationToken cancellationToken = default) =>
        // The plugin registers this command as "tag_rooms".
        revit.SendAsTextAsync("tag_rooms", new { useLeader, tagTypeId, roomIds }, "Room tagging failed", cancellationToken);
}

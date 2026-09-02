using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class TagAllWallsTool(RevitConnection revit)
{
    [McpServerTool(Name = "tag_all_walls")]
    [Description("Create tags for all walls in the current active view. Tags will be placed at the middle point of each wall.")]
    public Task<string> TagAllWallsAsync(
        [Description("Whether to use a leader line when creating the tags")]
        bool useLeader = false,
        [Description("The ID of the specific wall tag family type to use. If not provided, the default wall tag type will be used")]
        string? tagTypeId = null,
        CancellationToken cancellationToken = default) =>
        // The plugin registers this command as "tag_walls".
        revit.SendAsTextAsync("tag_walls", new { useLeader, tagTypeId }, "Wall tagging failed", cancellationToken);
}

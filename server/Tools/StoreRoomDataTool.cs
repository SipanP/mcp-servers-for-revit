using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RevitMcpServer.Data;

namespace RevitMcpServer.Tools;

#pragma warning disable IDE1006

[McpServerToolType]
public sealed class StoreRoomDataTool(RevitDataStore store)
{
    [McpServerTool(Name = "store_room_data")]
    [Description("Store or update room metadata for a specific Revit project in the local database. Rooms are linked to a project by project name. The project must exist before storing room data.")]
    public CallToolResult StoreRoomData(
        [Description("The name of the Revit project this room belongs to")] string project_name,
        [Description("Array of room data to store")] IReadOnlyList<RoomRecord> rooms)
    {
        try
        {
            var project = store.GetProjectByName(project_name);
            if (project is null)
            {
                return ToolResults.Error(
                    $"Project \"{project_name}\" not found. Please store project data first using store_project_data tool.");
            }

            var projectId = Convert.ToInt64(project["id"]);
            var stored = store.StoreRooms(projectId, rooms);
            var allRooms = store.GetRoomsByProjectId(projectId);

            return ToolResults.Json(new Dictionary<string, object?>
            {
                ["success"] = true,
                ["message"] = $"Stored {stored} room(s) successfully",
                ["project_id"] = projectId,
                ["project_name"] = project_name,
                ["total_rooms"] = allRooms.Count,
                ["rooms_stored"] = stored
            });
        }
        catch (Exception ex)
        {
            return ToolResults.Error(ex.Message);
        }
    }
}

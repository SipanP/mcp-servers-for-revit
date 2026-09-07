using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RevitMcpServer.Data;
using RevitMcpServer.Models;

namespace RevitMcpServer.Tools;

#pragma warning disable IDE1006

[McpServerToolType]
public sealed class QueryStoredDataTool(RevitDataStore store)
{
    [McpServerTool(Name = "query_stored_data")]
    [Description("Query stored Revit project and room data from the local database. Supports various query types: get all projects, get project by ID/name, get rooms by project, get all rooms, or get database statistics.")]
    public CallToolResult QueryStoredData(
        [Description("Type of query to perform")] StoredDataQueryType query_type,
        [Description("Project ID (required for 'project_by_id' and 'rooms_by_project_id')")]
        double? project_id = null,
        [Description("Project name (required for 'project_by_name' and 'rooms_by_project_name')")]
        string? project_name = null)
    {
        try
        {
            object? data;

            switch (query_type)
            {
                case StoredDataQueryType.all_projects:
                    data = store.GetAllProjects();
                    break;

                case StoredDataQueryType.project_by_id:
                {
                    var id = Require(project_id, "project_id");
                    var project = store.GetProjectById(id);
                    if (project is null)
                    {
                        return ToolResults.Failure($"Project with ID {id} not found");
                    }

                    data = project;
                    break;
                }

                case StoredDataQueryType.project_by_name:
                {
                    var name = Require(project_name, "project_name");
                    var project = store.GetProjectByName(name);
                    if (project is null)
                    {
                        return ToolResults.Failure($"Project \"{name}\" not found");
                    }

                    data = project;
                    break;
                }

                case StoredDataQueryType.rooms_by_project_id:
                    data = store.GetRoomsByProjectId(Require(project_id, "project_id"));
                    break;

                case StoredDataQueryType.rooms_by_project_name:
                {
                    var name = Require(project_name, "project_name");
                    var project = store.GetProjectByName(name);
                    if (project is null)
                    {
                        return ToolResults.Failure($"Project \"{name}\" not found");
                    }

                    data = store.GetRoomsByProjectId(Convert.ToInt64(project["id"]));
                    break;
                }

                case StoredDataQueryType.all_rooms:
                    data = store.GetAllRoomsWithProject();
                    break;

                case StoredDataQueryType.stats:
                    data = store.GetStats();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(query_type), $"Unknown query type: {query_type}");
            }

            return ToolResults.Json(new Dictionary<string, object?>
            {
                ["success"] = true,
                ["query_type"] = query_type,
                ["data"] = data
            });
        }
        catch (Exception ex)
        {
            return ToolResults.Error(ex.Message);
        }
    }

    private static long Require(double? value, string parameterName) =>
        value is null
            ? throw new ArgumentException($"{parameterName} is required for this query type", parameterName)
            : (long)value.Value;

    private static string Require(string? value, string parameterName) =>
        string.IsNullOrEmpty(value)
            ? throw new ArgumentException($"{parameterName} is required for this query type", parameterName)
            : value;
}

using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RevitMcpServer.Data;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

// The parameter names below are snake_case on purpose: they are the tool's public schema and
// must stay byte-identical to the schema the previous TypeScript server published.
#pragma warning disable IDE1006

[McpServerToolType]
public sealed class StoreProjectDataTool(RevitDataStore store)
{
    [McpServerTool(Name = "store_project_data")]
    [Description("Store or update Revit project metadata in the local database. This captures project information with a timestamp for later retrieval.")]
    public CallToolResult StoreProjectData(
        [Description("The name of the Revit project")] string project_name,
        [Description("File path to the project")] string? project_path = null,
        [Description("Project number or identifier")] string? project_number = null,
        [Description("Project address or location")] string? project_address = null,
        [Description("Client name")] string? client_name = null,
        [Description("Project status (e.g., Active, Completed, On Hold)")] string? project_status = null,
        [Description("Project author or creator")] string? author = null,
        [Description("Additional project metadata as key-value pairs")]
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        try
        {
            var projectId = store.StoreProject(new ProjectRecord
            {
                ProjectName = project_name,
                ProjectPath = project_path,
                ProjectNumber = project_number,
                ProjectAddress = project_address,
                ClientName = client_name,
                ProjectStatus = project_status,
                Author = author,
                Metadata = metadata
            });

            return ToolResults.Json(new Dictionary<string, object?>
            {
                ["success"] = true,
                ["message"] = "Project data stored successfully",
                ["project_id"] = projectId,
                ["project"] = store.GetProjectByName(project_name)
            });
        }
        catch (Exception ex)
        {
            return ToolResults.Error(ex.Message);
        }
    }
}

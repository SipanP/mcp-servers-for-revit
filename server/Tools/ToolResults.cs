using ModelContextProtocol.Protocol;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

/// <summary>Builds the JSON-in-text tool results the database-backed tools return.</summary>
internal static class ToolResults
{
    public static CallToolResult Json<T>(T payload) => Text(RevitJson.Render(payload), isError: false);

    /// <summary>A handled "not found" answer: reported as data, not as a tool error.</summary>
    public static CallToolResult Failure(string error) =>
        Text(RevitJson.Render(new Dictionary<string, object?>
        {
            ["success"] = false,
            ["error"] = error
        }), isError: false);

    /// <summary>An unexpected failure, flagged with <c>isError</c>.</summary>
    public static CallToolResult Error(string error) =>
        Text(RevitJson.Render(new Dictionary<string, object?>
        {
            ["success"] = false,
            ["error"] = error
        }), isError: true);

    private static CallToolResult Text(string text, bool isError) => new()
    {
        Content = [new TextContentBlock { Text = text }],
        IsError = isError
    };
}

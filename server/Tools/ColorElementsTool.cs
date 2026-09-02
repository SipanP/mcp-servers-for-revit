using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class ColorElementsTool(RevitConnection revit)
{
    [McpServerTool(Name = "color_elements")]
    [Description("Color elements in the current view based on a category and parameter value. Each unique parameter value gets assigned a distinct color.")]
    public async Task<string> ColorElementsAsync(
        [Description("The name of the Revit category to color (e.g., 'Walls', 'Doors', 'Rooms')")]
        string categoryName,
        [Description("The name of the parameter to use for grouping and coloring elements")]
        string parameterName,
        [Description("Whether to use a gradient color scheme instead of random colors")]
        bool useGradient = false,
        [Description("Optional array of custom RGB colors to use for specific parameter values")]
        IReadOnlyList<RgbColorSpec>? customColors = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // The plugin registers this command as "color_splash".
            var response = await revit.SendAsync(
                "color_splash",
                new { categoryName, parameterName, useGradient, customColors },
                cancellationToken);

            if (!IsSuccess(response))
            {
                var message = TryGetString(response, "message") ?? "Unknown error from Revit";
                return $"Color operation failed: {message}";
            }

            return FormatSuccess(response);
        }
        catch (Exception ex) when (ex is RevitConnectionException or RevitCommandException)
        {
            return $"Color operation failed: {ex.Message}";
        }
    }

    private static bool IsSuccess(JsonElement response) =>
        response.ValueKind is JsonValueKind.Object
        && response.TryGetProperty("success", out var success)
        && success.ValueKind is JsonValueKind.True;

    private static string FormatSuccess(JsonElement response)
    {
        var totalElements = TryGetNumberText(response, "totalElements");
        var coloredGroups = TryGetNumberText(response, "coloredGroups");

        var text = new StringBuilder()
            .Append($"Successfully colored {totalElements} elements across {coloredGroups} groups.")
            .AppendLine()
            .AppendLine()
            .AppendLine("Parameter Value Groups:");

        if (response.TryGetProperty("results", out var results) && results.ValueKind is JsonValueKind.Array)
        {
            foreach (var group in results.EnumerateArray())
            {
                var parameterValue = TryGetString(group, "parameterValue") ?? string.Empty;
                var count = TryGetNumberText(group, "count");
                var r = "0";
                var g = "0";
                var b = "0";

                if (group.TryGetProperty("color", out var color) && color.ValueKind is JsonValueKind.Object)
                {
                    r = TryGetNumberText(color, "r");
                    g = TryGetNumberText(color, "g");
                    b = TryGetNumberText(color, "b");
                }

                text.AppendLine($"- \"{parameterValue}\": {count} elements colored with RGB({r}, {g}, {b})");
            }
        }

        return text.ToString();
    }

    private static string? TryGetString(JsonElement element, string property) =>
        element.ValueKind is JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static string TryGetNumberText(JsonElement element, string property) =>
        element.ValueKind is JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind is JsonValueKind.Number
            ? value.GetDouble().ToString("0.############", CultureInfo.InvariantCulture)
            : "undefined";
}

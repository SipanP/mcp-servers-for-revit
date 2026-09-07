using System.Text.Json;
using System.Text.Json.Serialization;

namespace RevitMcpServer.Revit;

/// <summary>Serializer settings for talking to Revit and for rendering tool output.</summary>
internal static class RevitJson
{
    /// <summary>
    /// Outgoing command parameters. Null properties are omitted so the payload matches what the
    /// previous JavaScript server sent (JS drops <c>undefined</c> properties).
    /// </summary>
    public static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Tool output, matching the previous server's <c>JSON.stringify(response, null, 2)</c>.</summary>
    public static readonly JsonSerializerOptions Pretty = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string Render(JsonElement element) => JsonSerializer.Serialize(element, Pretty);

    public static string Render<T>(T value) => JsonSerializer.Serialize(value, Pretty);
}

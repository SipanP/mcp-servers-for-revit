namespace RevitMcpServer.Tools;

/// <summary>
/// Reproduces the <c>value || fallback</c> defaulting the previous JavaScript server applied to
/// numeric limits before sending them to Revit. JavaScript treats <c>0</c> as falsy, so an
/// explicit <c>0</c> fell back to the default rather than being forwarded.
/// </summary>
internal static class JsDefaults
{
    public static double Or(double? value, double fallback) =>
        value is null or 0 ? fallback : value.Value;
}

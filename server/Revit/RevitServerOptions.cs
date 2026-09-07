namespace RevitMcpServer.Revit;

/// <summary>
/// Connection settings for the Revit plugin's socket service.
/// Bound from environment variables prefixed with <c>REVIT_MCP_</c>.
/// </summary>
public sealed class RevitServerOptions
{
    /// <summary>Host the Revit plugin listens on. Env: <c>REVIT_MCP_HOST</c>.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Port the Revit plugin listens on. Env: <c>REVIT_MCP_PORT</c>.
    /// Must match the port hard-wired in <c>plugin/Core/SocketService.cs</c>.
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>Seconds to wait for the TCP connection. Env: <c>REVIT_MCP_CONNECTTIMEOUTSECONDS</c>.</summary>
    public int ConnectTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Seconds to wait for a command response. Revit commands run on the UI thread
    /// via an ExternalEvent, so slow operations on large models are normal.
    /// Env: <c>REVIT_MCP_REQUESTTIMEOUTSECONDS</c>.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 120;

    public TimeSpan ConnectTimeout => TimeSpan.FromSeconds(ConnectTimeoutSeconds);

    public TimeSpan RequestTimeout => TimeSpan.FromSeconds(RequestTimeoutSeconds);
}

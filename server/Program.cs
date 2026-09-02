using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RevitMcpServer.Data;
using RevitMcpServer.Revit;

var builder = Host.CreateApplicationBuilder(args);

// stdio is the MCP transport: anything written to stdout that is not a protocol frame
// corrupts the session, so every log record has to go to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(consoleOptions => consoleOptions.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Configuration.AddEnvironmentVariables("REVIT_MCP_");
builder.Services.Configure<RevitServerOptions>(builder.Configuration);

builder.Services.AddSingleton<RevitConnection>();
builder.Services.AddSingleton<RevitDataStore>();

builder.Services
    .AddMcpServer(serverOptions =>
    {
        serverOptions.ServerInfo = new ModelContextProtocol.Protocol.Implementation
        {
            Name = "mcp-server-for-revit",
            Version = ThisAssembly.Version
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

internal static class ThisAssembly
{
    public static string Version =>
        typeof(RevitConnection).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}

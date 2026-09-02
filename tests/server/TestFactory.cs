using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tests;

internal static class TestFactory
{
    /// <summary>A connection pointed at a <see cref="FakeRevitServer"/>, with short timeouts.</summary>
    public static RevitConnection ConnectionFor(FakeRevitServer server) =>
        new(Options.Create(new RevitServerOptions
        {
            Host = "127.0.0.1",
            Port = server.Port,
            ConnectTimeoutSeconds = 5,
            RequestTimeoutSeconds = 10
        }), NullLogger<RevitConnection>.Instance);

    /// <summary>A connection pointed at a port nothing is listening on.</summary>
    public static RevitConnection UnreachableConnection() =>
        new(Options.Create(new RevitServerOptions
        {
            Host = "127.0.0.1",
            Port = 1,
            ConnectTimeoutSeconds = 2,
            RequestTimeoutSeconds = 2
        }), NullLogger<RevitConnection>.Instance);

    /// <summary>Walks up from the test binaries to the repository root.</summary>
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "command.json")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new InvalidOperationException("Could not locate the repository root (no command.json found).");
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RevitMcpServer.Revit;

/// <summary>
/// Serialises access to Revit and turns command results into tool output.
/// </summary>
/// <remarks>
/// Revit executes commands on its UI thread through a single ExternalEvent, so concurrent
/// requests are not useful and were previously prevented by a module-level promise mutex.
/// The semaphore here plays that role.
/// </remarks>
public sealed class RevitConnection(IOptions<RevitServerOptions> options, ILogger<RevitConnection> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly RevitServerOptions _options = options.Value;

    /// <summary>Runs a command and returns the raw JSON-RPC result. Throws on failure.</summary>
    public async Task<JsonElement> SendAsync(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            logger.LogDebug("Sending '{Method}' to Revit at {Host}:{Port}", method, _options.Host, _options.Port);

            await using var client = await RevitClient
                .ConnectAsync(_options.Host, _options.Port, _options.ConnectTimeout, cancellationToken)
                .ConfigureAwait(false);

            return await client
                .InvokeAsync(method, parameters, _options.RequestTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Runs a command and renders the result as indented JSON. On failure the message is returned
    /// as ordinary tool text prefixed with <paramref name="failurePrefix"/>, matching the previous
    /// server's behaviour.
    /// </summary>
    public async Task<string> SendAsTextAsync(
        string method,
        object? parameters,
        string failurePrefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SendAsync(method, parameters, cancellationToken).ConfigureAwait(false);
            return RevitJson.Render(result);
        }
        catch (Exception ex) when (ex is RevitConnectionException or RevitCommandException)
        {
            logger.LogWarning(ex, "Revit command '{Method}' failed", method);
            return $"{failurePrefix}: {ex.Message}";
        }
    }
}

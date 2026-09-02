using System.Net.Sockets;
using System.Text.Json;

namespace RevitMcpServer.Revit;

/// <summary>
/// A single-use JSON-RPC 2.0 conversation with the Revit plugin's socket service
/// (<c>plugin/Core/SocketService.cs</c>).
/// </summary>
/// <remarks>
/// The wire protocol is deliberately unchanged from the previous TypeScript server so that
/// no plugin-side change is needed:
/// <list type="number">
///   <item>open a TCP connection per command,</item>
///   <item>write one JSON-RPC request object with no framing (no length prefix, no delimiter),</item>
///   <item>read bytes until the accumulated buffer parses as complete JSON — that <i>is</i> the framing,</item>
///   <item>close the connection.</item>
/// </list>
/// Note the plugin reads each request with a single 8192-byte <c>NetworkStream.Read</c>, so a request
/// larger than that (or one split across TCP segments) fails plugin-side. That limitation predates
/// this server and fixing it requires a coordinated plugin change.
/// </remarks>
internal sealed class RevitClient : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;

    private RevitClient(TcpClient tcp)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
    }

    public static async Task<RevitClient> ConnectAsync(
        string host,
        int port,
        TimeSpan connectTimeout,
        CancellationToken cancellationToken)
    {
        var tcp = new TcpClient();
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(connectTimeout);

        try
        {
            await tcp.ConnectAsync(host, port, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            tcp.Dispose();
            throw new RevitConnectionException(
                $"Timed out after {connectTimeout.TotalSeconds:0.#}s connecting to Revit at {host}:{port}. " +
                "Check that Revit is running and the MCP plugin's socket service is started.");
        }
        catch (SocketException ex)
        {
            tcp.Dispose();
            throw new RevitConnectionException(
                $"Could not connect to Revit at {host}:{port}. " +
                "Check that Revit is running and the MCP plugin's socket service is started.", ex);
        }

        return new RevitClient(tcp);
    }

    /// <summary>Sends one command and returns the JSON-RPC <c>result</c>.</summary>
    public async Task<JsonElement> InvokeAsync(
        string method,
        object? parameters,
        TimeSpan requestTimeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(requestTimeout);

        var requestId = Guid.NewGuid().ToString("N");
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new JsonRpcRequest(method, parameters ?? new object(), requestId),
            RevitJson.Wire);

        try
        {
            await _stream.WriteAsync(payload, timeoutSource.Token).ConfigureAwait(false);
            await _stream.FlushAsync(timeoutSource.Token).ConfigureAwait(false);

            var response = await ReadResponseAsync(timeoutSource.Token).ConfigureAwait(false);
            return ReadResult(response, method);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RevitConnectionException(
                $"Revit did not answer '{method}' within {requestTimeout.TotalSeconds:0.#}s. " +
                "The command may still be running in Revit.");
        }
        catch (IOException ex)
        {
            throw new RevitConnectionException($"Lost the connection to Revit while running '{method}'.", ex);
        }
    }

    /// <summary>
    /// Reads until the accumulated bytes form one complete JSON document. This mirrors the
    /// previous server's "retry JSON.parse until it succeeds" framing exactly.
    /// </summary>
    private async Task<JsonElement> ReadResponseAsync(CancellationToken cancellationToken)
    {
        var chunk = new byte[8192];
        using var accumulated = new MemoryStream();

        while (true)
        {
            var read = await _stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new RevitConnectionException(accumulated.Length == 0
                    ? "Revit closed the connection without answering."
                    : "Revit closed the connection before sending a complete response.");
            }

            accumulated.Write(chunk, 0, read);

            var buffered = accumulated.GetBuffer().AsMemory(0, (int)accumulated.Length);
            if (TryParse(buffered, out var response))
            {
                return response;
            }
        }
    }

    /// <summary>
    /// Parses the buffer if it holds a complete document. The result is cloned so it does not
    /// alias the accumulation buffer, which is reused and discarded as reading continues.
    /// </summary>
    private static bool TryParse(ReadOnlyMemory<byte> utf8, out JsonElement response)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8);
            response = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            // Not a complete document yet; keep reading.
            response = default;
            return false;
        }
    }

    private static JsonElement ReadResult(JsonElement response, string method)
    {
        if (response.TryGetProperty("error", out var error) && error.ValueKind is not JsonValueKind.Null)
        {
            var message = error.TryGetProperty("message", out var m) && m.ValueKind is JsonValueKind.String
                ? m.GetString()!
                : $"Unknown error from Revit running '{method}'";
            var code = error.TryGetProperty("code", out var c) && c.TryGetInt32(out var parsed) ? parsed : 0;
            throw new RevitCommandException(message, code);
        }

        return response.TryGetProperty("result", out var result)
            ? result
            : throw new RevitConnectionException($"Revit's response to '{method}' contained neither a result nor an error.");
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync().ConfigureAwait(false);
        _tcp.Dispose();
    }

    private sealed record JsonRpcRequest(string Method, object Params, string Id)
    {
        // ReSharper disable once UnusedMember.Local - serialized as part of the JSON-RPC envelope.
        public string Jsonrpc => "2.0";
    }
}

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RevitMcpServer.Tests;

/// <summary>
/// Stands in for the Revit plugin's socket service: accepts a connection, reads one JSON-RPC
/// request using the same "read until it parses" framing the plugin relies on, and writes back a
/// scripted response — optionally in small chunks, to exercise reassembly on the client side.
/// </summary>
internal sealed class FakeRevitServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _acceptLoop;
    private readonly List<string> _requests = [];
    private readonly Lock _sync = new();

    private FakeRevitServer(TcpListener listener, Func<JsonElement, string> respond, int chunkSize)
    {
        _listener = listener;
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(() => AcceptAsync(respond, chunkSize, _shutdown.Token));
    }

    public int Port { get; }

    /// <summary>Every request payload received, in order.</summary>
    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (_sync)
            {
                return [.. _requests];
            }
        }
    }

    public JsonElement LastRequest => JsonDocument.Parse(Requests[^1]).RootElement;

    public static FakeRevitServer Start(Func<JsonElement, string> respond, int chunkSize = int.MaxValue)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new FakeRevitServer(listener, respond, chunkSize);
    }

    /// <summary>Answers with a JSON-RPC success envelope carrying <paramref name="resultJson"/>.</summary>
    public static FakeRevitServer StartReturning(string resultJson, int chunkSize = int.MaxValue) =>
        Start(request => Envelope(request, "\"result\":" + resultJson), chunkSize);

    /// <summary>Answers with a JSON-RPC error envelope.</summary>
    public static FakeRevitServer StartFailing(string message, int code = -32603) =>
        Start(request => Envelope(
            request,
            "\"error\":{\"code\":" + code + ",\"message\":" + JsonSerializer.Serialize(message) + "}"));

    private static string Envelope(JsonElement request, string body) =>
        "{\"jsonrpc\":\"2.0\",\"id\":" +
        JsonSerializer.Serialize(request.GetProperty("id").GetString()) + "," + body + "}";

    private async Task AcceptAsync(Func<JsonElement, string> respond, int chunkSize, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(client, respond, chunkSize, cancellationToken), CancellationToken.None);
        }
    }

    private async Task ServeAsync(TcpClient client, Func<JsonElement, string> respond, int chunkSize, CancellationToken cancellationToken)
    {
        using (client)
        {
            await using var stream = client.GetStream();
            var buffer = new byte[8192];
            using var accumulated = new MemoryStream();

            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    return;
                }

                accumulated.Write(buffer, 0, read);

                JsonDocument request;
                try
                {
                    request = JsonDocument.Parse(accumulated.GetBuffer().AsMemory(0, (int)accumulated.Length));
                }
                catch (JsonException)
                {
                    continue;
                }

                using (request)
                {
                    lock (_sync)
                    {
                        _requests.Add(Encoding.UTF8.GetString(accumulated.GetBuffer(), 0, (int)accumulated.Length));
                    }

                    accumulated.SetLength(0);
                    var payload = Encoding.UTF8.GetBytes(respond(request.RootElement));

                    for (var offset = 0; offset < payload.Length; offset += chunkSize)
                    {
                        var length = Math.Min(chunkSize, payload.Length - offset);
                        await stream.WriteAsync(payload.AsMemory(offset, length), cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                    }
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }

        _shutdown.Dispose();
    }
}

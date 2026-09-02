namespace RevitMcpServer.Revit;

/// <summary>Raised when the server cannot reach or talk to the Revit plugin.</summary>
public sealed class RevitConnectionException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>Raised when Revit answers with a JSON-RPC error object.</summary>
public sealed class RevitCommandException(string message, int code) : Exception(message)
{
    public int Code { get; } = code;
}

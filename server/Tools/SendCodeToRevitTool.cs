using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Models;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class SendCodeToRevitTool(RevitConnection revit)
{
    [McpServerTool(Name = "send_code_to_revit")]
    [Description("Send C# code to Revit for execution. The code will be inserted into a template with access to the Revit Document and parameters. Your code should be written to work within the Execute method of the template.")]
    public async Task<string> SendCodeToRevitAsync(
        [Description("The C# code to execute in Revit. This code will be inserted into the Execute method of a template with access to Document and parameters.")]
        string code,
        [Description("Optional execution parameters that will be passed to your code")]
        IReadOnlyList<string>? parameters = null,
        [Description("How the snippet should interact with Revit transactions. Use 'auto' to wrap the snippet in a transaction, or 'none' when the called code manages its own transactions.")]
        TransactionMode transactionMode = TransactionMode.auto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await revit.SendAsync(
                "send_code_to_revit",
                new { code, parameters = parameters ?? [], transactionMode },
                cancellationToken);

            return $"Code execution successful!{Environment.NewLine}Result: {RevitJson.Render(response)}";
        }
        catch (Exception ex) when (ex is RevitConnectionException or RevitCommandException)
        {
            return $"Code execution failed: {ex.Message}";
        }
    }
}

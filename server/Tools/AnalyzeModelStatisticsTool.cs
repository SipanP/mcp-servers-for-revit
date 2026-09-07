using System.ComponentModel;
using ModelContextProtocol.Server;
using RevitMcpServer.Revit;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public sealed class AnalyzeModelStatisticsTool(RevitConnection revit)
{
    [McpServerTool(Name = "analyze_model_statistics")]
    [Description("Analyze model complexity with element counts. Returns detailed statistics about the Revit model including total element counts, total types, total families, views, sheets, counts by category (with type/family breakdown), and level-by-level element distribution. Useful for model auditing, performance analysis, and understanding model composition.")]
    public Task<string> AnalyzeModelStatisticsAsync(
        [Description("Whether to include detailed breakdown by family and type within each category. Defaults to true.")]
        bool includeDetailedTypes = true,
        CancellationToken cancellationToken = default) =>
        revit.SendAsTextAsync(
            "analyze_model_statistics",
            new { includeDetailedTypes },
            "Analyze model statistics failed",
            cancellationToken);
}

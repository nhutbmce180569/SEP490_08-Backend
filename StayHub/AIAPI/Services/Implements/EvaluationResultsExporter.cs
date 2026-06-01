using System.Globalization;
using System.Text;
using System.Text.Json;
using AIAPI.DTOs;

namespace AIAPI.Services.Implements;

public class EvaluationResultsExporter : IEvaluationResultsExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<EvaluationExportInfoDTO> ExportAsync(
        EvaluationRunResponseDTO result,
        string format,
        CancellationToken cancellationToken = default)
    {
        var normalized = string.IsNullOrWhiteSpace(format) ? "json" : format.Trim().ToLowerInvariant();
        var directory = Path.Combine(AppContext.BaseDirectory, "EvaluationResults");
        Directory.CreateDirectory(directory);

        var stamp = result.ExecutedAt.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var baseName = $"evaluation_{result.ProtocolVersion}_{stamp}";

        string filePath;
        if (normalized == "csv")
        {
            filePath = Path.Combine(directory, $"{baseName}.csv");
            await File.WriteAllTextAsync(filePath, BuildCsv(result), cancellationToken);
        }
        else
        {
            filePath = Path.Combine(directory, $"{baseName}.json");
            var json = JsonSerializer.Serialize(result, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }

        return new EvaluationExportInfoDTO
        {
            Format = normalized,
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };
    }

    private static string BuildCsv(EvaluationRunResponseDTO result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("section,strategy_key,strategy_name,ndcg_at_k,precision_at_k,recall_at_k,avg_group_satisfaction,avg_min_persona,avg_dissatisfaction_var,avg_envy_gap,avg_ild,constraint_rate");

        foreach (var row in result.BaselineResults)
        {
            sb.AppendLine(string.Join(",",
                "baseline",
                Csv(row.StrategyKey),
                Csv(row.StrategyName),
                F(row.NdcgAtK),
                F(row.PrecisionAtK),
                F(row.RecallAtK),
                F(row.AvgGroupSatisfaction),
                F(row.AvgMinPersonaUtility),
                F(row.AvgDissatisfactionVariance),
                F(row.AvgEnvyGap),
                F(row.AvgIntraListDiversity),
                F(row.ConstraintSatisfactionRate)));
        }

        if (result.AlphaSweepResults != null)
        {
            foreach (var row in result.AlphaSweepResults)
            {
                sb.AppendLine(string.Join(",",
                    "alpha_sweep",
                    Csv($"alpha={row.Alpha:0.00}"),
                    "",
                    F(row.NdcgAtK),
                    "",
                    "",
                    "",
                    F(row.AvgMinPersonaUtility),
                    F(row.AvgDissatisfactionVariance),
                    F(row.AvgEnvyGap),
                    "",
                    ""));
            }
        }

        if (result.SignificanceTests != null)
        {
            foreach (var row in result.SignificanceTests)
            {
                sb.AppendLine(string.Join(",",
                    "significance",
                    Csv(row.BaselineKey),
                    Csv(row.Metric),
                    F(row.MeanDelta),
                    F(row.PValueApprox),
                    row.ProposedBetter ? "1" : "0",
                    "",
                    "",
                    "",
                    "",
                    "",
                    ""));
            }
        }

        return sb.ToString();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}

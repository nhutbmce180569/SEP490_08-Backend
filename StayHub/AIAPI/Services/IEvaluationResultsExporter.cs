namespace AIAPI.Services;

public interface IEvaluationResultsExporter
{
    Task<DTOs.EvaluationExportInfoDTO> ExportAsync(
        DTOs.EvaluationRunResponseDTO result,
        string format,
        CancellationToken cancellationToken = default);
}

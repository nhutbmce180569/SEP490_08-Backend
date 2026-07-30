using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IRecommenderEvaluationService
{
    BaselineCatalogDTO GetBaselineCatalog();
    Task<EvaluationRunResponseDTO> RunOfflineEvaluationAsync(
        EvaluationRunRequestDTO request,
        CancellationToken cancellationToken = default);
    Task<WeightCalibrationResultDTO> CalibrateDimensionWeightsAsync(
        CancellationToken cancellationToken = default);
    Task<RagCorpusAblationResultDTO> RunRagCorpusAblationAsync(
        CancellationToken cancellationToken = default);
}

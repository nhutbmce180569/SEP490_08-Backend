using AIAPI.DTOs;
using AIAPI.ML;

namespace AIAPI.Services;

public interface ICatalogSyncService
{
    Task SyncCatalogAsync(CancellationToken cancellationToken = default);
}

public interface IModelTrainingService
{
    Task<TrainedModelBundle> RetrainAsync(CancellationToken cancellationToken = default);
    Task<ModelTrainingStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface ITourRecommendationService
{
    Task<List<TourRecommendationItemDTO>> RecommendAsync(int? customerId, int top, ParsedQueryDTO? hints = null, CancellationToken cancellationToken = default);
    Task<List<TourRecommendationItemDTO>> RecommendSimilarAsync(int tourId, int top, CancellationToken cancellationToken = default);
}

public interface ITourSemanticSearchService
{
    Task<List<TourSearchResultItemDTO>> SearchAsync(NaturalLanguageSearchRequestDTO request, CancellationToken cancellationToken = default);
    Task<List<TourismInsightDTO>> SearchTourismAsync(string query, string? city, int top, CancellationToken cancellationToken = default);
}

public interface ITourAssistantService
{
    Task<ChatResponseDTO> ChatAsync(string message, string? sessionId, int? customerId, CancellationToken cancellationToken = default);
    Task<List<TourRecommendationItemDTO>> ConsultAsync(TourConsultationRequestDTO request, int? customerId, CancellationToken cancellationToken = default);
    Task LogInteractionAsync(int? customerId, LogInteractionRequestDTO request, CancellationToken cancellationToken = default);
}

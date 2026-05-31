using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IPersonalizedTourRecommendationService
{
    StandardQuestionnaireDTO GetStandardQuestionnaire();
    ScoringModelDocumentationDTO GetScoringDocumentation();
    Task<PersonalizedRecommendationResponseDTO> RecommendFromProfileAsync(
        TourPreferenceQuestionnaireDTO profile,
        int? customerId,
        CancellationToken cancellationToken = default);
}

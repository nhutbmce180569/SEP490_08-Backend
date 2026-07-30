using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IPersonalizedTourRecommendationService
{
    StandardQuestionnaireDTO GetStandardQuestionnaire();
    ScoringModelDocumentationDTO GetScoringDocumentation();
    Task<PersonalizedRecommendationResponseDTO> RecommendFromProfileAsync(
        TourPreferenceQuestionnaireDTO profile,
        CancellationToken cancellationToken = default);
}

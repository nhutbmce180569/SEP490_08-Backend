using AIAPI.Models.Knowledge;

namespace AIAPI.Services;

public interface IRagKnowledgeIndex
{
    bool IsReady { get; }
    RagCorpusBundle Corpus { get; }
    void Initialize();
    IReadOnlyList<RagRetrievalResult> Retrieve(
        string query,
        string? city,
        IEnumerable<string>? interests,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren,
        int topK = 6);
    float ScoreTourCulturalFit(int tourId, string? tourCity, DTOs.TourPreferenceQuestionnaireDTO profile);
}

using AIAPI.Models.Knowledge;

namespace AIAPI.Services;

public interface ICulturalKnowledgeService
{
    Task<IReadOnlyList<CulturalFactResult>> GetFactsAsync(
        string? city,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren,
        IEnumerable<string>? interests = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<CulturalSourceRef> GetRegisteredSources();

    RagCorpusStatsDTO GetCorpusStats(int contentApiTourismCount);

    IReadOnlyList<RagRetrievalResult> SearchRag(
        string query,
        string? city,
        IEnumerable<string>? interests,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren,
        int topK = 8);

    IReadOnlyList<string> GetForeignVisitorNotesForCity(string? city);
}

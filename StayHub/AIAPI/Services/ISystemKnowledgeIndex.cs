using AIAPI.Models.Knowledge;

namespace AIAPI.Services;

public interface ISystemKnowledgeIndex
{
    bool IsReady { get; }
    int EntryCount { get; }
    void Initialize();
    IReadOnlyList<SystemKnowledgeHit> Retrieve(string query, int topK = 4);
}

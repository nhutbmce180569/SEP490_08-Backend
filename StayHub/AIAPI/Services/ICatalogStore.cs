using AIAPI.Models.Catalog;

namespace AIAPI.Services;

public interface ICatalogStore
{
    IReadOnlyList<TourCatalogItem> Tours { get; }
    IReadOnlyList<TourismKnowledgeItem> TourismItems { get; }
    DateTime? LastSyncedAt { get; }
    bool IsReady { get; }
    void Update(IReadOnlyList<TourCatalogItem> tours, IReadOnlyList<TourismKnowledgeItem> tourismItems);
}

using AIAPI.Models.Catalog;

namespace AIAPI.Services;

public sealed class CatalogStoreStats
{
    public int BaseTourCount { get; init; }
    public int AugmentedTourCount { get; init; }
    public int TotalTourCount { get; init; }
    public int RejectedByJaccard { get; init; }
    public double AvgPairwiseJaccardSample { get; init; }
    public bool AugmentationEnabled { get; init; }
}

public interface ICatalogStore
{
    IReadOnlyList<TourCatalogItem> Tours { get; }
    IReadOnlyList<TourismKnowledgeItem> TourismItems { get; }
    CatalogStoreStats? Stats { get; }
    DateTime? LastSyncedAt { get; }
    bool IsReady { get; }
    void Update(IReadOnlyList<TourCatalogItem> tours, IReadOnlyList<TourismKnowledgeItem> tourismItems, CatalogStoreStats? stats = null);
}

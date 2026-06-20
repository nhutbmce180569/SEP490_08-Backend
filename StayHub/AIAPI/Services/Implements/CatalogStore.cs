using AIAPI.Models.Catalog;

namespace AIAPI.Services.Implements;

public class CatalogStore : ICatalogStore
{
    private readonly object _lock = new();
    private List<TourCatalogItem> _tours = new();
    private List<TourismKnowledgeItem> _tourism = new();
    private readonly ILocalEmbeddingService _embeddingService;

    public CatalogStore(ILocalEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public IReadOnlyList<TourCatalogItem> Tours
    {
        get { lock (_lock) { return _tours; } }
    }

    public IReadOnlyList<TourismKnowledgeItem> TourismItems
    {
        get { lock (_lock) { return _tourism; } }
    }

    public CatalogStoreStats? Stats { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public bool IsReady { get; private set; }

    public void Update(
        IReadOnlyList<TourCatalogItem> tours,
        IReadOnlyList<TourismKnowledgeItem> tourismItems,
        CatalogStoreStats? stats = null)
    {
        var processedTours = tours.ToList();
        
        // Compute Semantic Embeddings for all tours
        foreach (var tour in processedTours)
        {
            if (tour.SemanticEmbedding == null)
            {
                tour.SemanticEmbedding = _embeddingService.EmbedText(tour.SearchDocument);
            }
        }

        lock (_lock)
        {
            _tours = processedTours;
            _tourism = tourismItems.ToList();
            Stats = stats;
            LastSyncedAt = DateTime.UtcNow;
            IsReady = _tours.Count > 0;
        }
    }
}

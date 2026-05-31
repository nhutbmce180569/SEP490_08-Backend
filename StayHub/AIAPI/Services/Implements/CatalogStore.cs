using AIAPI.Models.Catalog;

namespace AIAPI.Services.Implements;

public class CatalogStore : ICatalogStore
{
    private readonly object _lock = new();
    private List<TourCatalogItem> _tours = new();
    private List<TourismKnowledgeItem> _tourism = new();

    public IReadOnlyList<TourCatalogItem> Tours
    {
        get { lock (_lock) { return _tours; } }
    }

    public IReadOnlyList<TourismKnowledgeItem> TourismItems
    {
        get { lock (_lock) { return _tourism; } }
    }

    public DateTime? LastSyncedAt { get; private set; }
    public bool IsReady { get; private set; }

    public void Update(IReadOnlyList<TourCatalogItem> tours, IReadOnlyList<TourismKnowledgeItem> tourismItems)
    {
        lock (_lock)
        {
            _tours = tours.ToList();
            _tourism = tourismItems.ToList();
            LastSyncedAt = DateTime.UtcNow;
            IsReady = _tours.Count > 0;
        }
    }
}

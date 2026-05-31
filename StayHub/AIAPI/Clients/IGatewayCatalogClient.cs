using AIAPI.Models.Catalog;

namespace AIAPI.Clients;

public interface IGatewayCatalogClient
{
    Task<IReadOnlyList<TourCatalogItem>> FetchActiveToursAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourismKnowledgeItem>> FetchActiveTourismAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> FetchWishlistTourIdsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> FetchBookingTourIdsAsync(int customerId, CancellationToken cancellationToken = default);
}

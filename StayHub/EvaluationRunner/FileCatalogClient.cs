using System.Text.Json;
using AIAPI.Clients;
using AIAPI.Models.Catalog;

namespace EvaluationRunner;

public sealed class FileCatalogClient : IGatewayCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<IReadOnlyList<TourCatalogItem>> FetchActiveToursAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "evaluation-catalog.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Missing evaluation catalog JSON.", path);
        }

        var payload = JsonSerializer.Deserialize<CatalogFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Invalid catalog file.");

        foreach (var tour in payload.Tours)
        {
            if (string.IsNullOrWhiteSpace(tour.SearchDocument))
            {
                tour.SearchDocument = string.Join(" ",
                    new[] { tour.Name, tour.Description, tour.Country, tour.City, tour.Address }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        return Task.FromResult<IReadOnlyList<TourCatalogItem>>(payload.Tours);
    }

    public Task<IReadOnlyList<TourismKnowledgeItem>> FetchActiveTourismAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TourismKnowledgeItem>>(Array.Empty<TourismKnowledgeItem>());

    public Task<IReadOnlyList<int>> FetchWishlistTourIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());

    public Task<IReadOnlyList<int>> FetchBookingTourIdsAsync(int customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());

    private sealed class CatalogFile
    {
        public List<TourCatalogItem> Tours { get; set; } = [];
    }
}

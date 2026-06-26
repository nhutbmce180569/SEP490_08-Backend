using System.Net.Http.Json;
using System.Text.Json;
using AIAPI.Models.Catalog;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Clients;

public class GatewayCatalogClient : IGatewayCatalogClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GatewayCatalogClient(HttpClient httpClient, IOptions<GatewaySettings> gatewayOptions)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(gatewayOptions.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<IReadOnlyList<TourCatalogItem>> FetchActiveToursAsync(CancellationToken cancellationToken = default)
    {
        var allTours = new List<ExternalTourDTO>();
        const int pageSize = 50;
        var page = 1;
        var totalPages = 1;

        while (page <= totalPages)
        {
            var response = await _httpClient.GetAsync(
                $"api/tours/search?page={page}&pageSize={pageSize}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            var payload = await response.Content.ReadFromJsonAsync<PaginationResponse<ExternalTourDTO>>(JsonOptions, cancellationToken);
            if (payload?.Data == null || payload.Data.Count == 0)
            {
                break;
            }

            allTours.AddRange(payload.Data);
            totalPages = Math.Max(1, payload.TotalPages);
            page++;
        }

        return allTours
            .Where(t => string.Equals(t.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(MapTour)
            .ToList();
    }

    public async Task<IReadOnlyList<TourismKnowledgeItem>> FetchActiveTourismAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<TourismKnowledgeItem>();
        const int pageSize = 100;
        var page = 1;
        var totalPages = 1;

        while (page <= totalPages)
        {
            var response = await _httpClient.GetAsync(
                $"api/tourisminformation/active?page={page}&pageSize={pageSize}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                break;
            }

            var payload = await response.Content.ReadFromJsonAsync<PaginationResponse<ExternalTourismInformationDTO>>(JsonOptions, cancellationToken);
            if (payload?.Data == null || payload.Data.Count == 0)
            {
                break;
            }

            all.AddRange(payload.Data.Select(MapTourism));
            totalPages = Math.Max(1, payload.TotalPages);
            page++;
        }

        return all;
    }

    private static TourCatalogItem MapTour(ExternalTourDTO tour)
    {
        var itineraryTitles = tour.TourItineraries?
            .Select(i => i.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!)
            .ToList() ?? new List<string>();

        var tourismIds = tour.TourItineraries?
            .Where(i => i.TourismInfoId.HasValue)
            .Select(i => i.TourismInfoId!.Value)
            .Distinct()
            .ToList() ?? new List<int>();

        long? minPrice = null;
        long? maxPrice = null;
        DateTime? nextDeparture = null;
        int? durationDays = tour.TourItineraries?.Count;

        if (tour.TourSchedules != null && tour.TourSchedules.Count > 0)
        {
            nextDeparture = tour.TourSchedules.Min(s => s.DepartureDate);
            var scheduleDurations = tour.TourSchedules
                .Select(s => (s.ReturnDate.Date - s.DepartureDate.Date).Days + 1)
                .ToList();
            if (scheduleDurations.Count > 0)
            {
                durationDays ??= scheduleDurations.Min();
            }

            var prices = tour.TourSchedules
                .SelectMany(s => s.TourScheduleTickets ?? new List<ExternalTourScheduleTicketDTO>())
                .Where(t => t.IsActive != false && t.AvailableQuantity > 0)
                .Select(t => t.Price)
                .ToList();

            if (prices.Count > 0)
            {
                minPrice = prices.Min();
                maxPrice = prices.Max();
            }
        }

        var searchDocument = string.Join(" ",
            new[]
            {
                tour.Name,
                tour.Description,
                tour.Country,
                tour.City,
                tour.Address,
                tour.SourceName,
                tour.SourceUrl,
                string.Join(" ", itineraryTitles)
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var firstValidLocation = tour.TourItineraries?.FirstOrDefault(i => i.LocationLat.HasValue && i.LocationLng.HasValue);
        double? latitude = firstValidLocation?.LocationLat;
        double? longitude = firstValidLocation?.LocationLng;

        if (tour.City == "Dong Hai")
        {
            Console.WriteLine($"[Gateway Mapping] Tour='{tour.Name}', ItinerariesCount={tour.TourItineraries?.Count}, FirstValidLat={latitude}");
        }

        return new TourCatalogItem
        {
            Id = tour.Id,
            CategoryId = tour.CategoryId,
            Name = tour.Name,
            Description = tour.Description,
            Country = tour.Country,
            City = tour.City,
            Latitude = latitude,
            Longitude = longitude,
            Address = tour.Address,
            ImageUrl = tour.ImageUrl,
            SourceName = tour.SourceName,
            SourceUrl = tour.SourceUrl,
            Status = tour.Status ?? "Active",
            AverageStar = tour.AverageStar,
            ReviewCount = tour.Reviews?.Count ?? 0,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            NextDeparture = nextDeparture,
            DurationDays = durationDays,
            ItineraryTitles = itineraryTitles,
            TourismInfoIds = tourismIds,
            SearchDocument = searchDocument.Trim()
        };
    }

    private static TourismKnowledgeItem MapTourism(ExternalTourismInformationDTO item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Type = item.Type,
        Description = item.Description,
        City = item.City,
        Country = item.Country,
        SourceName = item.SourceName,
        SourceUrl = item.SourceUrl,
        SearchDocument = string.Join(" ", new[]
            {
                item.Name,
                item.Type,
                item.Description,
                item.City,
                item.Country,
                item.SourceName,
                item.SourceUrl
            }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim()
    };
}

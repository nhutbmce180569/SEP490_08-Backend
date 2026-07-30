using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models.Catalog;

namespace AIAPI.Services.Implements;

public class TourSemanticSearchService : ITourSemanticSearchService
{
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly QueryEntityExtractor _entityExtractor;
    private readonly IAiLocalizedCopy _text;

    public TourSemanticSearchService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        QueryEntityExtractor entityExtractor,
        IAiLocalizedCopy text)
    {
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _entityExtractor = entityExtractor;
        _text = text;
    }

    public Task<List<TourSearchResultItemDTO>> SearchAsync(NaturalLanguageSearchRequestDTO request, CancellationToken cancellationToken = default)
    {
        EnsureReady();

        var parsed = _entityExtractor.Merge(_entityExtractor.Extract(request.Query, _catalogStore), request);
        var filter = BuildFilter(parsed);

        Console.WriteLine($"[Debug SearchAsync] Query: '{request.Query}', City: '{request.City}', StartDate: '{request.StartDate}', EndDate: '{request.EndDate}', Top: {request.Top}");
        Console.WriteLine($"[Debug SearchAsync] Parsed City: '{parsed.City}', StartDate: '{parsed.StartDate}', EndDate: '{parsed.EndDate}', MinPrice: '{parsed.MinPrice}', MaxPrice: '{parsed.MaxPrice}', DurationDays: '{parsed.DurationDays}'");
        Console.WriteLine($"[Debug SearchAsync] Catalog tours count: {_catalogStore.Tours.Count}");

        int matchingFilterCount = 0;
        foreach (var tour in _catalogStore.Tours)
        {
            var categoryMatch = !parsed.CategoryId.HasValue || tour.CategoryId == parsed.CategoryId.Value;
            var countryMatch = string.IsNullOrWhiteSpace(parsed.Country) || string.Equals(tour.Country, parsed.Country, StringComparison.OrdinalIgnoreCase);
            var cityMatch = string.IsNullOrWhiteSpace(parsed.City) || VietnameseTextNormalizer.CityEquals(tour.City, parsed.City);
            var priceMatch = (!parsed.MinPrice.HasValue || (tour.MaxPrice.HasValue && tour.MaxPrice.Value >= parsed.MinPrice.Value)) &&
                             (!parsed.MaxPrice.HasValue || (tour.MinPrice.HasValue && tour.MinPrice.Value <= parsed.MaxPrice.Value));
            var durationMatch = !parsed.DurationDays.HasValue || (tour.DurationDays.HasValue && tour.DurationDays.Value == parsed.DurationDays.Value);
            var startMatch = !parsed.StartDate.HasValue || !tour.NextDeparture.HasValue || tour.NextDeparture.Value.Date >= parsed.StartDate.Value.Date;
            var endMatch = !parsed.EndDate.HasValue || !tour.NextDeparture.HasValue || tour.NextDeparture.Value.Date <= parsed.EndDate.Value.Date;
            
            var pass = categoryMatch && countryMatch && cityMatch && priceMatch && durationMatch && startMatch && endMatch;
            if (pass) matchingFilterCount++;
            
            if (tour.City != null && tour.City.Contains("Nha Trang", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[Debug Tour] TourId: {tour.Id}, Name: '{tour.Name}', City: '{tour.City}', MinPrice: {tour.MinPrice}, MaxPrice: {tour.MaxPrice}, NextDeparture: {tour.NextDeparture}");
                Console.WriteLine($"  cityMatch: {cityMatch}, priceMatch: {priceMatch}, durationMatch: {durationMatch}, startMatch: {startMatch}, endMatch: {endMatch} -> Pass: {pass}");
            }
        }
        Console.WriteLine($"[Debug SearchAsync] Total matching filter count: {matchingFilterCount}");

        var semanticMatches = _modelRegistry.SearchTours(request.Query, request.Top * 3, filter);

        var keywordMatches = KeywordFallback(request.Query, parsed, request.Top * 3);
        var merged = MergeScores(semanticMatches, keywordMatches)
            .GroupBy(x => CatalogTourIds.ResolveBaseTourId(x.TourId))
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(x => x.Score)
            .Take(request.Top)
            .ToList();

        var results = merged.Select(x => MapSearchResult(x.TourId, x.Score, request.Query)).ToList();
        return Task.FromResult(results);
    }

    public Task<List<TourismInsightDTO>> SearchTourismAsync(string query, string? city, int top, CancellationToken cancellationToken = default)
    {
        EnsureReady();

        var matches = _modelRegistry.SearchTourism(query, top * 2, city);
        var results = matches
            .Select(m =>
            {
                var item = _catalogStore.TourismItems.FirstOrDefault(t => t.Id == m.TourismId);
                if (item == null)
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(city) &&
                    !VietnameseTextNormalizer.CityEquals(item.City, city))
                {
                    return null;
                }

                return new TourismInsightDTO
                {
                    Id = item.Id,
                    Name = item.Name,
                    Type = item.Type,
                    Description = item.Description,
                    City = item.City,
                    SourceName = item.SourceName,
                    SourceUrl = item.SourceUrl,
                    RelevanceScore = m.Score
                };
            })
            .Where(x => x != null)
            .Cast<TourismInsightDTO>()
            .Take(top)
            .ToList();

        return Task.FromResult(results);
    }

    private void EnsureReady()
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI models are not ready. Please wait for catalog sync and model training to complete.");
        }
    }

    private Func<TourCatalogItem, bool> BuildFilter(ParsedQueryDTO parsed) => tour =>
    {
        if (parsed.CategoryId.HasValue && tour.CategoryId != parsed.CategoryId.Value) return false;
        if (!string.IsNullOrWhiteSpace(parsed.Country) &&
            !string.Equals(tour.Country, parsed.Country, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(parsed.City) &&
            !VietnameseTextNormalizer.CityEquals(tour.City, parsed.City)) return false;
        if (parsed.MinPrice.HasValue && (!tour.MaxPrice.HasValue || tour.MaxPrice.Value < parsed.MinPrice.Value)) return false;
        if (parsed.MaxPrice.HasValue && (!tour.MinPrice.HasValue || tour.MinPrice.Value > parsed.MaxPrice.Value)) return false;
        if (parsed.DurationDays.HasValue && tour.DurationDays.HasValue && tour.DurationDays.Value != parsed.DurationDays.Value) return false;
        if (parsed.StartDate.HasValue && tour.NextDeparture.HasValue &&
            tour.NextDeparture.Value.Date < parsed.StartDate.Value.Date) return false;
        if (parsed.EndDate.HasValue && tour.NextDeparture.HasValue &&
            tour.NextDeparture.Value.Date > parsed.EndDate.Value.Date) return false;
        return true;
    };

    private IEnumerable<(int TourId, float Score)> KeywordFallback(string query, ParsedQueryDTO parsed, int top)
    {
        var terms = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return _catalogStore.Tours
            .Where(BuildFilter(parsed))
            .Select(t =>
            {
                var doc = t.SearchDocument.ToLowerInvariant();
                var hits = terms.Count(term => doc.Contains(term, StringComparison.Ordinal));
                return (t.Id, Score: hits / (float)Math.Max(terms.Length, 1));
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(top);
    }

    private static IEnumerable<(int TourId, float Score)> MergeScores(
        IReadOnlyList<(int TourId, float Score)> semantic,
        IEnumerable<(int TourId, float Score)> keyword)
    {
        var map = semantic.ToDictionary(x => x.TourId, x => x.Score);
        foreach (var item in keyword)
        {
            if (map.TryGetValue(item.TourId, out var existing))
            {
                map[item.TourId] = Math.Max(existing, item.Score) * 0.7f + item.Score * 0.3f;
            }
            else
            {
                map[item.TourId] = item.Score * 0.5f;
            }
        }

        return map.Select(kv => (kv.Key, kv.Value));
    }

    private TourSearchResultItemDTO MapSearchResult(int tourId, float score, string query)
    {
        var tour = _catalogStore.Tours.First(t => t.Id == tourId);
        var publicId = CatalogTourIds.ResolveBaseTourId(tour.Id);
        var display = _catalogStore.Tours.FirstOrDefault(t => t.Id == publicId) ?? tour;
        return new TourSearchResultItemDTO
        {
            TourId = publicId,
            Name = display.Name,
            City = display.City,
            Country = display.Country,
            ImageUrl = display.ImageUrl,
            AverageStar = display.AverageStar,
            MinPrice = display.MinPrice,
            DurationDays = display.DurationDays,
            SemanticScore = score,
            Score = score,
            Snippet = BuildSnippet(display, query),
            Reason = _text.IsVietnamese
                ? "Khớp ngữ nghĩa với truy vấn của bạn (ML.NET text featurization)."
                : "Semantically matches your query (ML.NET text featurization)."
        };
    }

    private static string BuildSnippet(TourCatalogItem tour, string query)
    {
        var source = tour.Description ?? string.Join(", ", tour.ItineraryTitles);
        if (string.IsNullOrWhiteSpace(source))
        {
            return tour.Name;
        }

        var term = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(term))
        {
            return source.Length > 160 ? source[..160] + "..." : source;
        }

        var index = source.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return source.Length > 160 ? source[..160] + "..." : source;
        }

        var start = Math.Max(0, index - 40);
        var length = Math.Min(source.Length - start, 160);
        return source.Substring(start, length) + (start + length < source.Length ? "..." : "");
    }
}

public class TourRecommendationService : ITourRecommendationService
{
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly IAiLocalizedCopy _text;

    public TourRecommendationService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        IAiLocalizedCopy text)
    {
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _text = text;
    }

    public Task<List<TourRecommendationItemDTO>> RecommendFromQueryAsync(
        int top,
        ParsedQueryDTO? hints = null,
        CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI models are not ready.");
        }

        var tours = _catalogStore.Tours.ToList();
        if (hints != null)
        {
            tours = tours.Where(BuildFilter(hints)).ToList();
        }

        var queryText = BuildQueryText(hints);
        var scored = new Dictionary<int, (float Score, string Reason)>();

        if (!string.IsNullOrWhiteSpace(queryText))
        {
            foreach (var match in _modelRegistry.SearchTours(queryText, tours.Count, BuildFilter(hints ?? new ParsedQueryDTO())))
            {
                scored[match.TourId] = (
                    match.Score * 0.75f + CatalogQualityScore(match.TourId) * 0.25f,
                    _text.ReasonQueryMatch);
            }
        }

        foreach (var tour in tours)
        {
            if (!scored.ContainsKey(tour.Id))
            {
                scored[tour.Id] = (CatalogQualityScore(tour.Id), _text.ReasonCatalogQuality);
            }
        }

        var result = scored
            .Where(kv => tours.Any(t => t.Id == kv.Key))
            .GroupBy(kv => CatalogTourIds.ResolveBaseTourId(kv.Key))
            .Select(g => g.OrderByDescending(kv => kv.Value.Score).First())
            .OrderByDescending(kv => kv.Value.Score)
            .Take(top)
            .Select(kv => MapRecommendation(kv.Key, kv.Value.Score, kv.Value.Reason))
            .ToList();

        return Task.FromResult(result);
    }

    public Task<List<TourRecommendationItemDTO>> RecommendSimilarAsync(int tourId, int top, CancellationToken cancellationToken = default)
    {
        var resolvedId = CatalogTourIds.ResolveBaseTourId(tourId);
        var source = _catalogStore.Tours.FirstOrDefault(t => t.Id == resolvedId || t.Id == tourId);
        if (source == null)
        {
            throw new InvalidOperationException("Tour not found in AI catalog.");
        }

        var sourcePublicId = CatalogTourIds.ResolveBaseTourId(source.Id);
        var matches = _modelRegistry.SearchTours(source.SearchDocument, top + 1)
            .Where(m => CatalogTourIds.ResolveBaseTourId(m.TourId) != sourcePublicId)
            .GroupBy(m => CatalogTourIds.ResolveBaseTourId(m.TourId))
            .Select(g => g.OrderByDescending(m => m.Score).First())
            .Take(top)
            .Select(m => MapRecommendation(m.TourId, m.Score, _text.ReasonSimilarTour(source.Name)))
            .ToList();

        return Task.FromResult(matches);
    }

    private static string BuildQueryText(ParsedQueryDTO? hints)
    {
        if (hints == null)
        {
            return "";
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(hints.City)) parts.Add(hints.City);
        if (!string.IsNullOrWhiteSpace(hints.Country)) parts.Add(hints.Country);
        if (hints.DurationDays.HasValue) parts.Add($"{hints.DurationDays.Value} days");
        if (hints.MaxPrice.HasValue) parts.Add($"budget {hints.MaxPrice.Value}");
        return string.Join(' ', parts);
    }

    private float CatalogQualityScore(int tourId)
    {
        var tour = _catalogStore.Tours.FirstOrDefault(t => t.Id == tourId);
        if (tour == null)
        {
            return 0f;
        }

        var ratingPart = (float)((tour.AverageStar ?? 3.0) / 5.0);
        var reviewPart = Math.Min(tour.ReviewCount / 20f, 1f);
        return ratingPart * 0.6f + reviewPart * 0.4f;
    }

    private static Func<TourCatalogItem, bool> BuildFilter(ParsedQueryDTO parsed) => tour =>
    {
        if (parsed.CategoryId.HasValue && tour.CategoryId != parsed.CategoryId.Value) return false;
        if (!string.IsNullOrWhiteSpace(parsed.Country) &&
            !string.Equals(tour.Country, parsed.Country, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(parsed.City) &&
            !VietnameseTextNormalizer.CityEquals(tour.City, parsed.City)) return false;
        if (parsed.MinPrice.HasValue && (!tour.MaxPrice.HasValue || tour.MaxPrice.Value < parsed.MinPrice.Value)) return false;
        if (parsed.MaxPrice.HasValue && (!tour.MinPrice.HasValue || tour.MinPrice.Value > parsed.MaxPrice.Value)) return false;
        if (parsed.DurationDays.HasValue && tour.DurationDays.HasValue && tour.DurationDays.Value != parsed.DurationDays.Value) return false;
        return true;
    };

    private TourRecommendationItemDTO MapRecommendation(int tourId, float score, string reason)
    {
        var tour = _catalogStore.Tours.First(t => t.Id == tourId);
        var publicId = CatalogTourIds.ResolveBaseTourId(tour.Id);
        var display = _catalogStore.Tours.FirstOrDefault(t => t.Id == publicId) ?? tour;
        return new TourRecommendationItemDTO
        {
            TourId = publicId,
            Name = display.Name,
            City = display.City,
            Country = display.Country,
            ImageUrl = display.ImageUrl,
            AverageStar = display.AverageStar,
            MinPrice = display.MinPrice,
            DurationDays = display.DurationDays,
            Score = score,
            Reason = reason
        };
    }
}

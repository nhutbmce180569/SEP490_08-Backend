using AIAPI.Models.Catalog;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AIAPI.Recommender;

public sealed class CatalogAugmentationResult
{
    public IReadOnlyList<TourCatalogItem> Catalog { get; init; } = Array.Empty<TourCatalogItem>();
    public int BaseTourCount { get; init; }
    public int VariantCount { get; init; }
    public int RejectedByJaccard { get; init; }
    public double AvgPairwiseJaccard { get; init; }
}

/// <summary>
/// Expands a base tour catalog with price/date/activity variants (paper synthesis protocol).
/// Synthetic IDs use <c>baseId * 10000 + variantIndex</c> to avoid collisions with real tours.
/// </summary>
public static class TourCatalogAugmentor
{
    private static readonly string[] ActivitySuffixes =
    [
        "sunrise viewpoint",
        "local food workshop",
        "guided photo walk",
        "community homestay",
        "eco cycling route",
        "heritage storytelling",
        "night market tour",
        "river cruise extension"
    ];

    public static CatalogAugmentationResult Augment(
        IReadOnlyList<TourCatalogItem> baseTours,
        int targetSize = EvaluationDataSpec.TargetAugmentedCatalogSize,
        int maxVariantsPerBase = EvaluationDataSpec.MaxVariantsPerBaseTour,
        double jaccardThreshold = EvaluationDataSpec.CatalogJaccardThreshold,
        double priceNoiseSigmaRatio = EvaluationDataSpec.PriceNoiseSigmaRatio,
        int departureShiftDaysMax = EvaluationDataSpec.DepartureShiftDaysMax,
        int randomSeed = EvaluationDataSpec.DefaultRandomSeed)
    {
        if (baseTours.Count == 0)
        {
            return new CatalogAugmentationResult();
        }

        var rng = new Random(randomSeed);
        var accepted = baseTours.Select(CloneBase).ToList();
        var documents = accepted.Select(Tokenize).ToList();
        var rejected = 0;

        foreach (var baseTour in baseTours)
        {
            for (var v = 1; v <= maxVariantsPerBase && accepted.Count < targetSize; v++)
            {
                var variant = CreateVariant(baseTour, v, rng, priceNoiseSigmaRatio, departureShiftDaysMax);
                var tokens = Tokenize(variant);

                if (IsTooSimilar(tokens, documents, jaccardThreshold))
                {
                    rejected++;
                    continue;
                }

                accepted.Add(variant);
                documents.Add(tokens);
            }
        }

        var avgJaccard = EstimateAvgPairwiseJaccard(documents, sampleSize: Math.Min(500, documents.Count));

        return new CatalogAugmentationResult
        {
            Catalog = accepted,
            BaseTourCount = baseTours.Count,
            VariantCount = accepted.Count - baseTours.Count,
            RejectedByJaccard = rejected,
            AvgPairwiseJaccard = avgJaccard
        };
    }

    private static TourCatalogItem CloneBase(TourCatalogItem t) => new()
    {
        Id = t.Id,
        CategoryId = t.CategoryId,
        Name = t.Name,
        Description = t.Description,
        Country = t.Country,
        City = t.City,
        Address = t.Address,
        ImageUrl = t.ImageUrl,
        SourceName = t.SourceName,
        SourceUrl = t.SourceUrl,
        Status = t.Status,
        AverageStar = t.AverageStar,
        ReviewCount = t.ReviewCount,
        MinPrice = t.MinPrice,
        MaxPrice = t.MaxPrice,
        NextDeparture = t.NextDeparture,
        DurationDays = t.DurationDays,
        SearchDocument = t.SearchDocument,
        ItineraryTitles = t.ItineraryTitles.ToList(),
        TourismInfoIds = t.TourismInfoIds.ToList()
    };

    private static TourCatalogItem CreateVariant(
        TourCatalogItem baseTour,
        int variantIndex,
        Random rng,
        double priceNoiseSigmaRatio,
        int departureShiftDaysMax)
    {
        var suffix = ActivitySuffixes[rng.Next(ActivitySuffixes.Length)];
        var desc = (baseTour.Description ?? baseTour.Name).TrimEnd('.');
        var variantDesc = $"{desc}. Optional add-on: {suffix}.";

        long? minPrice = baseTour.MinPrice;
        long? maxPrice = baseTour.MaxPrice;
        if (minPrice.HasValue && minPrice.Value > 0)
        {
            var noise = SampleGaussian(rng) * priceNoiseSigmaRatio;
            var factor = Math.Clamp(1.0 + noise, 0.65, 1.45);
            minPrice = (long)Math.Round(minPrice.Value * factor / 50_000d) * 50_000;
            maxPrice = maxPrice.HasValue
                ? (long)Math.Round(maxPrice.Value * factor / 50_000d) * 50_000
                : minPrice;
        }

        DateTime? departure = baseTour.NextDeparture;
        if (departure.HasValue)
        {
            var shift = rng.Next(-departureShiftDaysMax, departureShiftDaysMax + 1);
            departure = departure.Value.AddDays(shift);
        }

        var itineraries = baseTour.ItineraryTitles.ToList();
        if (itineraries.Count > 0)
        {
            itineraries[0] = $"{itineraries[0]} — {suffix}";
        }

        var searchDocument = string.Join(" ",
            new[] { baseTour.Name, variantDesc, baseTour.City, baseTour.Country, suffix, string.Join(" ", itineraries) }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        return new TourCatalogItem
        {
            Id = baseTour.Id * 10_000 + variantIndex,
            CategoryId = baseTour.CategoryId,
            Name = $"{baseTour.Name} (Variant {variantIndex})",
            Description = variantDesc,
            Country = baseTour.Country,
            City = baseTour.City,
            Address = baseTour.Address,
            ImageUrl = baseTour.ImageUrl,
            SourceName = baseTour.SourceName,
            SourceUrl = baseTour.SourceUrl,
            Status = baseTour.Status,
            AverageStar = baseTour.AverageStar,
            ReviewCount = Math.Max(0, baseTour.ReviewCount + rng.Next(-3, 8)),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            NextDeparture = departure,
            DurationDays = baseTour.DurationDays,
            ItineraryTitles = itineraries,
            TourismInfoIds = baseTour.TourismInfoIds.ToList(),
            SearchDocument = searchDocument.Trim()
        };
    }

    private static bool IsTooSimilar(
        HashSet<string> candidate,
        List<HashSet<string>> accepted,
        double threshold)
    {
        foreach (var existing in accepted)
        {
            if (Jaccard(candidate, existing) > threshold)
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> Tokenize(TourCatalogItem tour)
    {
        var text = $"{tour.Name} {tour.Description} {tour.SearchDocument}".ToLowerInvariant();
        var tokens = Regex.Split(text, @"\W+")
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.Ordinal);
        return tokens;
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0)
        {
            return 1.0;
        }

        var inter = a.Intersect(b).Count();
        var union = a.Union(b).Count();
        return union == 0 ? 0 : inter / (double)union;
    }

    private static double EstimateAvgPairwiseJaccard(List<HashSet<string>> documents, int sampleSize)
    {
        if (documents.Count < 2)
        {
            return 0;
        }

        var rng = new Random(42);
        double sum = 0;
        var samples = 0;

        for (var s = 0; s < sampleSize; s++)
        {
            var i = rng.Next(documents.Count);
            var j = rng.Next(documents.Count);
            if (i == j)
            {
                continue;
            }

            sum += Jaccard(documents[i], documents[j]);
            samples++;
        }

        return samples == 0 ? 0 : sum / samples;
    }

    private static double SampleGaussian(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}

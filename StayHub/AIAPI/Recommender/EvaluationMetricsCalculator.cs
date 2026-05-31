using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Models.Catalog;

namespace AIAPI.Recommender;

/// <summary>
/// Proxy relevance labels for offline evaluation when expert judgments are unavailable.
/// rel=0 irrelevant, 1 weak, 2 moderate, 3 strong match.
/// </summary>
public static class RelevanceLabeler
{
    public static Dictionary<int, int> LabelTours(TourPreferenceQuestionnaireDTO profile, IReadOnlyList<TourCatalogItem> tours)
    {
        var labels = new Dictionary<int, int>();
        foreach (var tour in tours)
        {
            labels[tour.Id] = ComputeRelevance(profile, tour);
        }

        return labels;
    }

    private static int ComputeRelevance(TourPreferenceQuestionnaireDTO profile, TourCatalogItem tour)
    {
        var score = 0;
        var doc = tour.SearchDocument.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(profile.PreferredCity) &&
            (VietnameseTextNormalizer.CityEquals(tour.City, profile.PreferredCity) ||
             VietnameseTextNormalizer.ContainsNormalized(tour.Name, profile.PreferredCity)))
        {
            score += 1;
        }

        var interestHits = profile.TravelInterests.Count(i =>
            doc.Contains(i, StringComparison.OrdinalIgnoreCase) ||
            InterestKeywordHit(doc, i));

        if (interestHits >= 2)
        {
            score += 2;
        }
        else if (interestHits == 1)
        {
            score += 1;
        }

        if (profile.MaxBudgetPerPerson.HasValue && tour.MinPrice.HasValue &&
            tour.MinPrice.Value <= profile.MaxBudgetPerPerson.Value)
        {
            score += 1;
        }

        if (profile.HasElderly && (doc.Contains("trek") || doc.Contains("motorbike")))
        {
            score = Math.Max(0, score - 2);
        }

        if (profile.HasChildren && (doc.Contains("dive") || doc.Contains("motorbike")))
        {
            score = Math.Max(0, score - 1);
        }

        return Math.Clamp(score, 0, 3);
    }

    private static bool InterestKeywordHit(string doc, string interest) => interest switch
    {
        "beach" => doc.Contains("beach") || doc.Contains("island"),
        "river" => doc.Contains("mekong") || doc.Contains("floating"),
        "culture" => doc.Contains("culture") || doc.Contains("ancient") || doc.Contains("heritage"),
        "food" => doc.Contains("food") || doc.Contains("cuisine"),
        "relax" => doc.Contains("resort") || doc.Contains("spa") || doc.Contains("cruise"),
        "nature" => doc.Contains("mountain") || doc.Contains("trek") || doc.Contains("cloud"),
        "adventure" => doc.Contains("adventure") || doc.Contains("trek") || doc.Contains("dive"),
        "photography" => doc.Contains("photo") || doc.Contains("sunset"),
        "city" => doc.Contains("city") || doc.Contains("market"),
        _ => doc.Contains(interest, StringComparison.OrdinalIgnoreCase)
    };
}

public static class EvaluationMetricsCalculator
{
    public static float NdcgAtK(IReadOnlyList<int> rankedTourIds, IReadOnlyDictionary<int, int> relevance, int k)
    {
        var dcg = 0d;
        for (var i = 0; i < Math.Min(k, rankedTourIds.Count); i++)
        {
            var rel = relevance.GetValueOrDefault(rankedTourIds[i], 0);
            dcg += (Math.Pow(2, rel) - 1) / Math.Log2(i + 2);
        }

        var ideal = relevance.Values.OrderByDescending(r => r).Take(k).ToList();
        var idcg = 0d;
        for (var i = 0; i < ideal.Count; i++)
        {
            idcg += (Math.Pow(2, ideal[i]) - 1) / Math.Log2(i + 2);
        }

        return idcg <= 0 ? 0f : (float)(dcg / idcg);
    }

    public static float PrecisionAtK(IReadOnlyList<int> rankedTourIds, IReadOnlyDictionary<int, int> relevance, int k)
    {
        var top = rankedTourIds.Take(k).ToList();
        if (top.Count == 0)
        {
            return 0f;
        }

        var hits = top.Count(id => relevance.GetValueOrDefault(id, 0) >= 2);
        return hits / (float)top.Count;
    }

    public static float RecallAtK(IReadOnlyList<int> rankedTourIds, IReadOnlyDictionary<int, int> relevance, int k)
    {
        var relevant = relevance.Count(kv => kv.Value >= 2);
        if (relevant == 0)
        {
            return 0f;
        }

        var hits = rankedTourIds.Take(k).Count(id => relevance.GetValueOrDefault(id, 0) >= 2);
        return hits / (float)relevant;
    }

    public static float IntraListDiversity(IReadOnlyList<TourCatalogItem> recommended)
    {
        if (recommended.Count <= 1)
        {
            return 0f;
        }

        var cities = recommended.Select(t => VietnameseTextNormalizer.Normalize(t.City ?? "")).Distinct().Count();
        return cities / (float)recommended.Count;
    }

    /// <summary>Approximate two-tailed p-value for paired differences (paper reporting aid).</summary>
    public static float ApproximatePairedPValue(IReadOnlyList<float> pairedDeltas)
    {
        if (pairedDeltas.Count < 2)
        {
            return 1f;
        }

        var mean = pairedDeltas.Average();
        var variance = pairedDeltas.Select(d => (d - mean) * (d - mean)).Sum() / (pairedDeltas.Count - 1);
        if (variance <= 0)
        {
            return mean == 0 ? 1f : (mean > 0 ? 0.05f : 0.95f);
        }

        var stdErr = MathF.Sqrt(variance / pairedDeltas.Count);
        if (stdErr <= 0)
        {
            return 1f;
        }

        var t = MathF.Abs(mean / stdErr);
        return t switch
        {
            >= 2.576f => 0.01f,
            >= 1.96f => 0.05f,
            >= 1.645f => 0.10f,
            _ => 0.20f
        };
    }

    /// <summary>Cohen's κ for nominal agreement on 0-3 relevance grades.</summary>
    public static float CohenKappa(IReadOnlyList<int> raterA, IReadOnlyList<int> raterB)
    {
        if (raterA.Count != raterB.Count || raterA.Count == 0)
        {
            return 0f;
        }

        var n = raterA.Count;
        var categories = Enumerable.Range(0, 4).ToList();
        var po = raterA.Zip(raterB, (a, b) => a == b ? 1f : 0f).Average();

        var pe = 0f;
        foreach (var c in categories)
        {
            var pa = raterA.Count(x => x == c) / (float)n;
            var pb = raterB.Count(x => x == c) / (float)n;
            pe += pa * pb;
        }

        return pe >= 1f ? 1f : (po - pe) / (1f - pe);
    }

    /// <summary>Linear weighted κ for ordinal 0-3 grades.</summary>
    public static float WeightedKappa(IReadOnlyList<int> raterA, IReadOnlyList<int> raterB)
    {
        if (raterA.Count != raterB.Count || raterA.Count == 0)
        {
            return 0f;
        }

        var n = raterA.Count;
        var weights = new float[4, 4];
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                weights[i, j] = 1f - Math.Abs(i - j) / 3f;
            }
        }

        var observed = 0f;
        for (var k = 0; k < n; k++)
        {
            observed += weights[raterA[k], raterB[k]];
        }

        observed /= n;

        var expected = 0f;
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                var pi = raterA.Count(x => x == i) / (float)n;
                var pj = raterB.Count(x => x == j) / (float)n;
                expected += weights[i, j] * pi * pj;
            }
        }

        return expected >= 1f ? 1f : (observed - expected) / (1f - expected);
    }

    public static float WilcoxonSignedRankPApprox(IReadOnlyList<float> pairedDeltas)
    {
        if (pairedDeltas.Count < 5)
        {
            return ApproximatePairedPValue(pairedDeltas);
        }

        var nonZero = pairedDeltas.Where(d => Math.Abs(d) > 1e-6f).Select(Math.Abs).OrderBy(x => x).ToList();
        if (nonZero.Count == 0)
        {
            return 1f;
        }

        var wPlus = 0d;
        for (var i = 0; i < nonZero.Count; i++)
        {
            var rank = i + 1;
            if (pairedDeltas.Count(d => Math.Abs(d) == nonZero[i]) > 0 && pairedDeltas.Any(d => d > 0 && Math.Abs(d) == nonZero[i]))
            {
                wPlus += rank;
            }
        }

        var n = nonZero.Count;
        var mean = n * (n + 1) / 4.0;
        var std = Math.Sqrt(n * (n + 1) * (2 * n + 1) / 24.0);
        if (std <= 0)
        {
            return 1f;
        }

        var z = Math.Abs((wPlus - mean) / std);
        return z switch
        {
            >= 2.576 => 0.01f,
            >= 1.96 => 0.05f,
            >= 1.645 => 0.10f,
            _ => 0.20f
        };
    }
}

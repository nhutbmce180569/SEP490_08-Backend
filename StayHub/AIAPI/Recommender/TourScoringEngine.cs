using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.ML;
using AIAPI.Models.Catalog;
using AIAPI.Recommender;
using AIAPI.Services;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Recommender;

public class TourScoringResult
{
    public float FairnessScore { get; set; }
    public float MinPersonaScore { get; set; }
    public float MeanPersonaScore { get; set; }
    public Dictionary<string, float> PersonaScores { get; set; } = new();
    public Dictionary<string, float> DimensionScores { get; set; } = new();
    public List<string> MatchReasons { get; set; } = new();
    public bool PassesHardConstraints { get; set; }
    public float EnvyGap { get; set; }
    public float DissatisfactionVariance { get; set; }
}

public class TourScoringEngine
{
    private readonly RecommenderSettings _settings;
    private readonly IRagKnowledgeIndex _ragIndex;

    private static readonly Dictionary<string, string[]> InterestKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["beach"] = ["beach", "sea", "island", "resort", "biển", "đảo"],
        ["culture"] = ["culture", "heritage", "ancient", "temple", "unesco", "văn hóa", "di tích", "phố cổ"],
        ["nature"] = ["nature", "mountain", "trek", "cloud", "forest", "thiên nhiên", "núi"],
        ["food"] = ["food", "cuisine", "street food", "ẩm thực", "đặc sản", "seafood"],
        ["adventure"] = ["adventure", "trek", "climb", "dive", "mạo hiểm", "kayak"],
        ["relax"] = ["relax", "spa", "resort", "chill", "nghỉ dưỡng", "honeymoon", "cruise"],
        ["photography"] = ["photo", "sunset", "cloud hunting", "chụp ảnh"],
        ["city"] = ["city", "night market", "thành phố", "chợ đêm"],
        ["river"] = ["river", "mekong", "floating market", "sông", "chợ nổi", "delta"]
    };

    public TourScoringEngine(IOptions<RecommenderSettings> settings, IRagKnowledgeIndex ragIndex)
    {
        _settings = settings.Value;
        _ragIndex = ragIndex;
    }

    public TourScoringResult ScoreTour(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        IReadOnlyList<TravelPersona> personas,
        Dictionary<int, float> semanticScores,
        WeatherAdviceDTO? weather,
        bool includeKnowledgeDimensions = true)
    {
        var result = new TourScoringResult { PassesHardConstraints = PassesHardConstraints(tour, profile) };
        if (!result.PassesHardConstraints)
        {
            return result;
        }

        var dimensionScores = ComputeSharedDimensions(tour, profile, semanticScores, weather, includeKnowledgeDimensions);
        result.DimensionScores = dimensionScores;

        foreach (var persona in personas)
        {
            var personaScore = ScoreForPersona(tour, profile, persona, dimensionScores, result.MatchReasons, includeKnowledgeDimensions);
            result.PersonaScores[persona.PersonaType] = Clamp01(personaScore);
        }

        result.MinPersonaScore = result.PersonaScores.Count > 0 ? result.PersonaScores.Values.Min() : 0f;
        result.MeanPersonaScore = result.PersonaScores.Count > 0 ? result.PersonaScores.Values.Average() : 0f;
        result.DissatisfactionVariance = FairnessFormalization.DissatisfactionVariance(result.PersonaScores);
        result.EnvyGap = FairnessFormalization.EnvyGap(result.PersonaScores);

        return result;
    }

    private static bool PassesHardConstraints(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.PreferredCity) &&
            !VietnameseTextNormalizer.CityEquals(tour.City, profile.PreferredCity) &&
            !VietnameseTextNormalizer.ContainsNormalized(tour.Name, profile.PreferredCity) &&
            !VietnameseTextNormalizer.ContainsNormalized(tour.Address ?? "", profile.PreferredCity))
        {
            return false;
        }

        if (profile.MaxBudgetPerPerson.HasValue && tour.MinPrice.HasValue &&
            tour.MinPrice.Value > profile.MaxBudgetPerPerson.Value)
        {
            return false;
        }

        return true;
    }

    private Dictionary<string, float> ComputeSharedDimensions(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float> semanticScores,
        WeatherAdviceDTO? weather,
        bool includeKnowledge)
    {
        return new Dictionary<string, float>
        {
            ["location"] = ScoreLocation(tour, profile),
            ["budget"] = ScoreBudget(tour, profile),
            ["schedule"] = ScoreSchedule(tour, profile),
            ["interest_semantic"] = semanticScores.TryGetValue(tour.Id, out var s) ? Clamp01(s) : 0f,
            ["weather"] = ScoreWeather(tour, weather),
            ["accessibility"] = ScoreAccessibilityBase(tour),
            ["cultural_fit"] = includeKnowledge ? ScoreCulturalFit(tour, profile) : 0.5f
        };
    }

    private static float ScoreForPersona(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        TravelPersona persona,
        Dictionary<string, float> dimensions,
        List<string> reasons,
        bool includeKnowledge)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        float interestMatch = ScoreInterestMatch(doc, persona.Interests);
        float score =
            interestMatch * ScoringModelSpec.DimensionWeights.InterestSemantic +
            dimensions["location"] * ScoringModelSpec.DimensionWeights.Location +
            dimensions["budget"] * ScoringModelSpec.DimensionWeights.Budget +
            dimensions["schedule"] * ScoringModelSpec.DimensionWeights.Schedule +
            dimensions["weather"] * ScoringModelSpec.DimensionWeights.Weather +
            dimensions["cultural_fit"] * ScoringModelSpec.DimensionWeights.CulturalFit;

        var accessibility = dimensions["accessibility"];

        if (persona.RequiresAccessibility)
        {
            var elderlyFit = ScoreElderlyFit(doc, tour);
            accessibility = Math.Min(accessibility, elderlyFit);
            if (elderlyFit >= 0.6f)
            {
                reasons.Add($"[{persona.Label}] Lịch trình phù hợp người cao tuổi (điểm accessibility {elderlyFit:P0}).");
            }
            else if (elderlyFit < 0.4f)
            {
                reasons.Add($"[{persona.Label}] Không tối ưu cho người cao tuổi (trekking/leo núi/xe máy).");
            }
        }

        if (persona.RequiresFamilyFriendly)
        {
            var childFit = ScoreChildFit(doc, tour);
            accessibility = Math.Min(accessibility, childFit);
            if (childFit >= 0.6f)
            {
                reasons.Add($"[{persona.Label}] Thân thiện trẻ em (điểm family-fit {childFit:P0}).");
            }
            else if (childFit < 0.4f)
            {
                reasons.Add($"[{persona.Label}] Cân nhắc với trẻ em (hoạt động mạo hiểm/dài).");
            }
        }

        if (persona.RequiresInternationalGuidance && includeKnowledge)
        {
            var intl = ScoreInternationalFit(doc, tour);
            score += intl * ScoringModelSpec.DimensionWeights.CulturalFit;
            if (intl >= 0.5f)
            {
                reasons.Add($"[{persona.Label}] Phù hợp khách quốc tế — văn hóa/ trải nghiệm dễ tiếp cận.");
            }
        }

        if (persona.PersonaType == ScoringModelSpec.PersonaTypes.GroupDynamics)
        {
            score += ScoreGroupDynamics(doc, profile.CompanionType) * 0.08f;
        }

        score += accessibility * ScoringModelSpec.DimensionWeights.Accessibility;

        if (interestMatch >= 0.5f)
        {
            reasons.Add($"[{persona.Label}] Khớp sở thích ({string.Join(", ", persona.Interests)}).");
        }

        return Clamp01(score) * persona.Weight;
    }

    private static float ScoreLocation(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (string.IsNullOrWhiteSpace(profile.PreferredCity))
        {
            return 0.5f;
        }

        if (VietnameseTextNormalizer.CityEquals(tour.City, profile.PreferredCity))
        {
            return 1f;
        }

        if (VietnameseTextNormalizer.ContainsNormalized(tour.Name, profile.PreferredCity))
        {
            return 0.75f;
        }

        return 0f;
    }

    private static float ScoreBudget(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (!profile.MaxBudgetPerPerson.HasValue || !tour.MinPrice.HasValue)
        {
            return 0.5f;
        }

        var ratio = tour.MinPrice.Value / (float)profile.MaxBudgetPerPerson.Value;
        return ratio <= 1f ? 1f - ratio * 0.3f : 0f;
    }

    private static float ScoreSchedule(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (!tour.NextDeparture.HasValue)
        {
            return 0.4f;
        }

        var start = profile.PreferredStartDate.Date;
        var end = (profile.PreferredEndDate ?? profile.PreferredStartDate.AddDays(30)).Date;
        return tour.NextDeparture.Value.Date >= start && tour.NextDeparture.Value.Date <= end ? 1f : 0.3f;
    }

    private static float ScoreWeather(TourCatalogItem tour, WeatherAdviceDTO? weather)
    {
        if (weather == null)
        {
            return 0.5f;
        }

        var doc = tour.SearchDocument.ToLowerInvariant();
        var rainy = (weather.TotalRainMm ?? 0) >= 30;

        if (rainy && (doc.Contains("beach") || doc.Contains("island")))
        {
            return 0.25f;
        }

        if (rainy && (doc.Contains("floating market") || doc.Contains("market") || doc.Contains("museum")))
        {
            return 0.85f;
        }

        if (!rainy && (doc.Contains("beach") || doc.Contains("outdoor")))
        {
            return 0.9f;
        }

        return 0.6f;
    }

    private static float ScoreAccessibilityBase(TourCatalogItem tour)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        if (doc.Contains("trek") || doc.Contains("motorbike") || doc.Contains("climb"))
        {
            return 0.25f;
        }

        if (doc.Contains("cruise") || doc.Contains("garden") || doc.Contains("resort") || doc.Contains("floating market"))
        {
            return 0.85f;
        }

        return 0.6f;
    }

    private float ScoreCulturalFit(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (_ragIndex.IsReady)
        {
            var ragScore = _ragIndex.ScoreTourCulturalFit(tour.Id, tour.City, profile);
            if (ragScore >= 0.55f)
            {
                return ragScore;
            }
        }

        if (profile.TravelInterests.Contains("culture", StringComparer.OrdinalIgnoreCase) ||
            profile.NationalityType == TravelerNationalityTypes.Foreigner)
        {
            var doc = tour.SearchDocument.ToLowerInvariant();
            if (doc.Contains("heritage") || doc.Contains("ancient") || doc.Contains("culture") || doc.Contains("mekong"))
            {
                return 0.9f;
            }
        }

        return 0.5f;
    }

    private static float ScoreInterestMatch(string doc, IEnumerable<string> interests)
    {
        var list = interests.ToList();
        if (list.Count == 0)
        {
            return 0.5f;
        }

        int hits = 0;
        foreach (var interest in list)
        {
            if (!InterestKeywords.TryGetValue(interest, out var keys))
            {
                continue;
            }

            if (keys.Any(k => doc.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                hits++;
            }
        }

        return hits / (float)list.Count;
    }

    private static float ScoreElderlyFit(string doc, TourCatalogItem tour)
    {
        if (doc.Contains("trek") || doc.Contains("motorbike") || doc.Contains("climb"))
        {
            return 0.15f;
        }

        if (doc.Contains("cruise") || doc.Contains("floating market") || doc.Contains("ancient") || doc.Contains("garden"))
        {
            return 0.9f;
        }

        return (tour.DurationDays ?? 3) <= 2 ? 0.7f : 0.5f;
    }

    private static float ScoreChildFit(string doc, TourCatalogItem tour)
    {
        if (doc.Contains("motorbike") || doc.Contains("dive") || doc.Contains("trek"))
        {
            return 0.2f;
        }

        if (doc.Contains("beach") || doc.Contains("island") || doc.Contains("market") || doc.Contains("garden"))
        {
            return 0.85f;
        }

        return 0.55f;
    }

    private static float ScoreInternationalFit(string doc, TourCatalogItem tour)
    {
        float score = 0.4f;
        if (doc.Contains("mekong") || doc.Contains("floating market") || doc.Contains("ancient") || doc.Contains("unesco"))
        {
            score += 0.35f;
        }

        if (doc.Contains("food") || doc.Contains("cuisine"))
        {
            score += 0.15f;
        }

        if (VietnameseTextNormalizer.CityEquals(tour.City, "Can Tho"))
        {
            score += 0.1f;
        }

        return Clamp01(score);
    }

    private static float ScoreGroupDynamics(string doc, string companionType)
    {
        return companionType switch
        {
            TravelCompanionTypes.Couple when doc.Contains("romantic") || doc.Contains("honeymoon") || doc.Contains("sunset") || doc.Contains("cruise") => 1f,
            TravelCompanionTypes.Family when !doc.Contains("trek") && !doc.Contains("motorbike") => 0.8f,
            TravelCompanionTypes.Group when (doc.Contains("adventure") || doc.Contains("city")) => 0.75f,
            _ => 0.5f
        };
    }

    private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
}

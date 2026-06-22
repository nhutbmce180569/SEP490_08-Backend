using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
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
    private readonly IDimensionWeightProvider _weights;
    private readonly IRagKnowledgeIndex _ragIndex;
    private readonly IAiLocalizedCopy _text;

    public TourScoringEngine(
        IOptions<RecommenderSettings> settings,
        IDimensionWeightProvider weights,
        IRagKnowledgeIndex ragIndex,
        IAiLocalizedCopy text)
    {
        _settings = settings.Value;
        _weights = weights;
        _ragIndex = ragIndex;
        _text = text;
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
            var personaScore = ScoreForPersona(tour, profile, persona, dimensionScores, result.MatchReasons, includeKnowledgeDimensions, _weights, _text);
            result.PersonaScores[persona.PersonaType] = Clamp01(personaScore);
        }

        result.MinPersonaScore = result.PersonaScores.Count > 0 ? result.PersonaScores.Values.Min() : 0f;
        result.MeanPersonaScore = result.PersonaScores.Count > 0 ? result.PersonaScores.Values.Average() : 0f;
        result.DissatisfactionVariance = FairnessFormalization.DissatisfactionVariance(result.PersonaScores);
        result.EnvyGap = FairnessFormalization.EnvyGap(result.PersonaScores);

        AppendDimensionMatchReasons(result, tour, profile, dimensionScores);

        return result;
    }

    private void AppendDimensionMatchReasons(
        TourScoringResult result,
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<string, float> dimensions)
    {
        var candidates = new List<(float Score, string Reason)>();

        if (!string.IsNullOrWhiteSpace(profile.PreferredCity) && dimensions.TryGetValue("location", out var location) && location >= 0.75f)
        {
            candidates.Add((location, _text.ReasonLocationMatch(profile.PreferredCity)));
        }

        if (profile.MaxBudgetPerPerson.HasValue && dimensions.TryGetValue("budget", out var budget) && budget >= 0.7f)
        {
            candidates.Add((budget, _text.ReasonBudgetFit));
        }

        if (dimensions.TryGetValue("interest_semantic", out var interest) && interest >= 0.55f)
        {
            candidates.Add((interest, _text.ReasonInterestStrong));
        }

        if (dimensions.TryGetValue("schedule", out var schedule) && schedule >= 0.9f)
        {
            candidates.Add((schedule, _text.ReasonScheduleFit));
        }

        if (tour.AverageStar is >= 4.0f)
        {
            candidates.Add(((float)tour.AverageStar.Value / 5f, _text.ReasonGoodRating));
        }

        foreach (var (_, reason) in candidates
                     .OrderByDescending(x => x.Score)
                     .Where(x => result.MatchReasons.All(r => !string.Equals(r, x.Reason, StringComparison.OrdinalIgnoreCase)))
                     .Take(Math.Max(0, 4 - result.MatchReasons.Count)))
        {
            result.MatchReasons.Add(reason);
        }

        if (result.MatchReasons.Count == 0 && candidates.Count > 0)
        {
            result.MatchReasons.Add(candidates.OrderByDescending(x => x.Score).First().Reason);
        }
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
        var rawWeather = ScoreWeather(tour, weather);
        var season = ScoreSeason(tour, profile);
        var crowd = ScoreCrowd(tour, profile);

        var dict = new Dictionary<string, float>
        {
            ["location"] = ScoreLocation(tour, profile),
            ["budget"] = ScoreBudget(tour, profile),
            ["schedule"] = ScoreSchedule(tour, profile),
            ["interest_semantic"] = ComputeInterestDimensionScore(tour, profile, semanticScores),
            ["weather"] = (rawWeather + season + crowd) / 3.0f,
            ["accessibility"] = ScoreAccessibilityBase(tour),
            ["cultural_fit"] = includeKnowledge ? ScoreCulturalFit(tour, profile) : 0.5f
        };
        return dict;
    }

    private static float ScoreForPersona(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        TravelPersona persona,
        Dictionary<string, float> dimensions,
        List<string> reasons,
        bool includeKnowledge,
        IDimensionWeightProvider weights,
        IAiLocalizedCopy text)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        float interestMatch = ScoreInterestMatch(doc, persona.Interests);
        float score =
            interestMatch * weights.InterestSemantic +
            dimensions["location"] * weights.Location +
            dimensions["budget"] * weights.Budget +
            dimensions["schedule"] * weights.Schedule +
            dimensions["weather"] * weights.Weather + 
            dimensions["cultural_fit"] * weights.CulturalFit;

        var accessibility = dimensions["accessibility"];

        if (persona.RequiresAccessibility)
        {
            var elderlyFit = ScoreElderlyFit(doc, tour);
            accessibility = Math.Min(accessibility, elderlyFit);
            if (elderlyFit >= 0.6f)
            {
                reasons.Add(text.ReasonElderlyGood(persona.Label, elderlyFit));
            }
            else if (elderlyFit < 0.4f)
            {
                reasons.Add(text.ReasonElderlyPoor(persona.Label));
            }
        }

        if (persona.RequiresFamilyFriendly)
        {
            var childFit = ScoreChildFit(doc, tour);
            accessibility = Math.Min(accessibility, childFit);
            if (childFit >= 0.6f)
            {
                reasons.Add(text.ReasonChildGood(persona.Label, childFit));
            }
            else if (childFit < 0.4f)
            {
                reasons.Add(text.ReasonChildPoor(persona.Label));
            }
        }

        if (persona.RequiresInternationalGuidance && includeKnowledge)
        {
            var intl = ScoreInternationalFit(doc, tour);
            score += intl * weights.CulturalFit;
            if (intl >= 0.5f)
            {
                reasons.Add(text.ReasonInternationalGood(persona.Label));
            }
        }

        if (persona.PersonaType == ScoringModelSpec.PersonaTypes.GroupDynamics)
        {
            score += ScoreGroupDynamics(doc, profile.CompanionType) * 0.08f;
        }

        score += accessibility * weights.Accessibility;

        if (interestMatch >= 0.5f)
        {
            reasons.Add(text.ReasonInterestMatch(persona.Label, persona.Interests));
        }

        return Clamp01(score) * persona.Weight;
    }

    private static float ScoreLocation(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (string.IsNullOrWhiteSpace(profile.PreferredCity))
        {
            return 1f;
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
            return 1f;
        }

        var ratio = tour.MinPrice.Value / (float)profile.MaxBudgetPerPerson.Value;
        if (ratio > 1f)
        {
            return 0f;
        }

        // Peak score (1.0) around 70-100% of budget. If < 70%, it scales down to 0.4.
        if (ratio >= 0.7f)
        {
            return 1f;
        }
        
        return 0.4f + (ratio / 0.7f) * 0.6f;
    }

    private static float ScoreSchedule(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (!tour.NextDeparture.HasValue)
        {
            return 0.4f;
        }

        var start = profile.PreferredStartDate.Date;
        var end = (profile.PreferredEndDate ?? profile.PreferredStartDate.AddDays(30)).Date;
        
        var daysOutside = ScheduleAvailabilityHelper.DaysOutsideWindow(tour.NextDeparture, start, end);
        if (daysOutside == 0)
        {
            return 1f;
        }
        
        // Use a Gaussian decay curve for a smoother score drop-off
        // sigma = 14 days. 
        // 7 days outside -> ~88%
        // 14 days outside -> ~60%
        // 22 days outside -> ~29%
        double sigma = 14.0;
        double score = Math.Exp(-(daysOutside * daysOutside) / (2 * sigma * sigma));
        
        return (float)Math.Clamp(score, 0.0, 1.0);
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

    private static float ScoreSeason(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        // Dummy implementation for Season aware logic
        var doc = tour.SearchDocument.ToLowerInvariant();
        var month = profile.PreferredStartDate.Month;

        if (month is >= 5 and <= 8 && doc.Contains("beach"))
        {
            return 0.9f; // High score for summer beaches
        }

        if (month is >= 11 or <= 2 && doc.Contains("mountain"))
        {
            return 0.8f; // High score for winter mountains
        }

        return 0.5f; // Neutral
    }

    private static float ScoreCrowd(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        // Dummy implementation for Crowd aware logic
        var isHoliday = profile.PreferredStartDate.DayOfWeek == DayOfWeek.Saturday || 
                        profile.PreferredStartDate.DayOfWeek == DayOfWeek.Sunday;

        if (isHoliday && (tour.ReviewCount > 100))
        {
            return 0.4f; // Penalize crowded places on holidays
        }

        return 0.8f; // Otherwise good
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

        var doc = tour.SearchDocument.ToLowerInvariant();
        if (doc.Contains("heritage") || doc.Contains("ancient") || doc.Contains("culture") || doc.Contains("mekong") ||
            doc.Contains("văn hóa") || doc.Contains("bản sắc") || doc.Contains("di sản") || doc.Contains("lịch sử"))
        {
            return 0.9f;
        }

        return 0.5f;
    }

    private static float ComputeInterestDimensionScore(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float> semanticScores)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        var keywordScore = InterestMatchHelper.ScoreKeywordMatch(doc, profile.TravelInterests);
        var semanticScore = semanticScores.TryGetValue(tour.Id, out var s) ? Clamp01(s) : 0f;

        if (semanticScore <= 0f)
        {
            return keywordScore;
        }

        var blended = keywordScore * 0.55f + semanticScore * 0.45f;
        return Clamp01(Math.Max(keywordScore, blended));
    }

    private static float ScoreInterestMatch(string doc, IEnumerable<string> interests) =>
        InterestMatchHelper.ScoreKeywordMatch(doc, interests);

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

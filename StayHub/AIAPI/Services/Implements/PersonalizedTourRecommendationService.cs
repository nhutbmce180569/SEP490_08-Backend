using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models.Catalog;
using AIAPI.Models.Knowledge;
using AIAPI.Recommender;
using AIAPI.Services;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Services.Implements;

public class PersonalizedTourRecommendationService : IPersonalizedTourRecommendationService
{
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly IWeatherService _weatherService;
    private readonly ICulturalKnowledgeService _culturalKnowledge;
    private readonly TourRanker _tourRanker;
    private readonly RecommenderSettings _settings;
    private readonly IAiLocalizedCopy _text;
    private readonly IKnowledgeLocalizationService _knowledgeLocalizer;
    private readonly IDimensionWeightProvider _dimensionWeights;

    public PersonalizedTourRecommendationService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        IWeatherService weatherService,
        ICulturalKnowledgeService culturalKnowledge,
        TourRanker tourRanker,
        IOptions<RecommenderSettings> settings,
        IAiLocalizedCopy text,
        IKnowledgeLocalizationService knowledgeLocalizer,
        IDimensionWeightProvider dimensionWeights)
    {
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _weatherService = weatherService;
        _culturalKnowledge = culturalKnowledge;
        _tourRanker = tourRanker;
        _settings = settings.Value;
        _text = text;
        _knowledgeLocalizer = knowledgeLocalizer;
        _dimensionWeights = dimensionWeights;
    }

    public StandardQuestionnaireDTO GetStandardQuestionnaire() => new()
    {
        Version = "2.1",
        Questions =
        [
            new QuestionnaireFieldDTO
            {
                FieldKey = "companionType",
                Label = _text.QuestionCompanionType,
                InputType = "single_select",
                Required = true,
                Hint = _text.QuestionCompanionHint,
                Options =
                [
                    new() { Value = TravelCompanionTypes.Solo, Label = _text.OptionSolo },
                    new() { Value = TravelCompanionTypes.Couple, Label = _text.OptionCouple },
                    new() { Value = TravelCompanionTypes.Family, Label = _text.OptionFamily },
                    new() { Value = TravelCompanionTypes.Group, Label = _text.OptionGroup }
                ]
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "preferredStartDate",
                Label = _text.QuestionStartDate,
                InputType = "date",
                Required = true,
                Hint = _text.QuestionStartDateHint
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "preferredEndDate",
                Label = _text.QuestionEndDate,
                InputType = "date",
                Required = false
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "maxBudgetPerPerson",
                Label = _text.QuestionBudget,
                InputType = "number",
                Required = false
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "hasElderly",
                Label = _text.QuestionHasElderly,
                InputType = "boolean",
                Required = true
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "hasChildren",
                Label = _text.QuestionHasChildren,
                InputType = "boolean",
                Required = true
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "travelInterests",
                Label = _text.QuestionInterests,
                InputType = "multi_select",
                Required = true,
                Options =
                [
                    new() { Value = "beach", Label = _text.OptionBeach },
                    new() { Value = "culture", Label = _text.OptionCulture },
                    new() { Value = "nature", Label = _text.OptionNature },
                    new() { Value = "food", Label = _text.OptionFood },
                    new() { Value = "adventure", Label = _text.OptionAdventure },
                    new() { Value = "relax", Label = _text.OptionRelax },
                    new() { Value = "photography", Label = _text.OptionPhotography },
                    new() { Value = "city", Label = _text.OptionCity },
                    new() { Value = "river", Label = _text.OptionRiver }
                ]
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "nationalityType",
                Label = _text.QuestionNationality,
                InputType = "single_select",
                Required = true,
                Options =
                [
                    new() { Value = TravelerNationalityTypes.Vietnamese, Label = _text.OptionVietnamese },
                    new() { Value = TravelerNationalityTypes.Foreigner, Label = _text.OptionForeigner }
                ]
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "preferredCity",
                Label = _text.QuestionPreferredCity,
                InputType = "text",
                Required = false,
                Hint = _text.QuestionPreferredCityHint
            }
        ]
    };

    public ScoringModelDocumentationDTO GetScoringDocumentation() => new()
    {
        Specification = BuildTransparencyMeta(Array.Empty<string>()),
        MethodologySummary =
            "FCAHR — Fair Constraint-Aware Hybrid Recommender for group tour planning. " +
            "Primary contribution: U = α·min_p u_p + (1-α)·mean_p u_p with persona decomposition. " +
            "Multi-source knowledge augmentation (UNESCO-cited corpus, ContentAPI, Wikidata CC0). " +
            "Baselines available via GET /api/ai/evaluation/baselines and POST /api/ai/evaluation/run.",
        PaperTitleSuggestion = ScoringModelSpec.PaperTitleSuggestion,
        FormalDefinitions = new Dictionary<string, string>
        {
            ["persona_utility"] = ScoringModelSpec.FormalDefinitions.PersonaUtility,
            ["fcahr_utility"] = ScoringModelSpec.FormalDefinitions.CafhrUtility,
            ["min_persona_penalty"] = ScoringModelSpec.FormalDefinitions.MinPersonaPenalty,
            ["dissatisfaction_variance"] = ScoringModelSpec.FormalDefinitions.DissatisfactionVariance,
            ["envy_gap"] = ScoringModelSpec.FormalDefinitions.EnvyGap,
            ["mean_baseline"] = ScoringModelSpec.FormalDefinitions.MeanBaseline,
            ["least_misery_baseline"] = ScoringModelSpec.FormalDefinitions.LeastMiseryBaseline,
            ["borda_baseline"] = ScoringModelSpec.FormalDefinitions.BordaBaseline,
            ["evaluation_protocol"] = ScoringModelSpec.EvaluationProtocol.ProfileGeneration
        },
        Baselines = AggregationStrategies.GetAll()
            .Select(b => new BaselineInfoDTO
            {
                Key = b.Key,
                Name = b.Name,
                Description = b.Description,
                IsProposed = b.Key == AggregationStrategies.CafhrFair
            })
            .ToList()
    };

    public async Task<PersonalizedRecommendationResponseDTO> RecommendFromProfileAsync(
        TourPreferenceQuestionnaireDTO profile,
        int? customerId,
        CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI models are not ready.");
        }

        ValidateProfile(profile);

        var sessionId = string.IsNullOrWhiteSpace(profile.SessionId)
            ? Guid.NewGuid().ToString("N")
            : profile.SessionId;

        var personas = TravelPartyDecomposer.Decompose(profile, _text);
        var weatherCity = profile.PreferredCity ?? InferCityFromInterests(profile.TravelInterests);

        WeatherAdviceDTO? weather = null;
        if (!string.IsNullOrWhiteSpace(weatherCity))
        {
            weather = await _weatherService.GetTravelWeatherAdviceAsync(
                weatherCity, profile.PreferredStartDate, profile.PreferredEndDate, cancellationToken);
        }

        var interestQuery = InterestMatchHelper.BuildSearchQuery(profile.TravelInterests);
        var semanticScores = _modelRegistry.ComputeTourSemanticScores(interestQuery)
            .ToDictionary(x => x.Key, x => x.Value);

        var rankingPool = Math.Min(
            _catalogStore.Tours.Count,
            Math.Max(profile.Top * 5, 30));

        var ranked = _tourRanker.RankTours(
            _catalogStore.Tours.ToList(),
            WithRankingPool(profile, rankingPool),
            semanticScores,
            weather,
            AggregationStrategies.CafhrFair);

        var windowStart = profile.PreferredStartDate.Date;
        var windowEnd = ScheduleAvailabilityHelper.ResolveWindowEnd(profile.PreferredStartDate, profile.PreferredEndDate);

        var rankedDistinct = ranked
            .GroupBy(x => CatalogTourIds.ResolveBaseTourId(x.Tour.Id))
            .Select(g => g.OrderByDescending(x => x.Scoring.FairnessScore).First())
            .ToList();

        var mapped = rankedDistinct
            .Select(x => MapTour(x.Tour, x.Scoring, profile, windowStart, windowEnd))
            .ToList();

        var (exactTours, nearbyTours, scheduleAvailability) =
            BuildRecommendationLists(mapped, profile.Top, windowStart, windowEnd);

        var allowedCities = ResolveAllowedCities(exactTours, nearbyTours, profile);

        var culturalFactResults = await GetFactsForRecommendedToursAsync(
            allowedCities,
            profile,
            cancellationToken);

        var personaTypes = personas.Select(p => p.PersonaType).ToList();

        return new PersonalizedRecommendationResponseDTO
        {
            SessionId = sessionId,
            AppliedProfile = profile,
            WeatherAdvice = weather,
            ScheduleAvailability = scheduleAvailability,
            RecommendedTours = exactTours,
            NearbyScheduleTours = nearbyTours,
            RelatedInsights = MapCulturalInsights(culturalFactResults).Select(_knowledgeLocalizer.LocalizeInsight).ToList(),
            CulturalFacts = culturalFactResults.Select(MapFactDto).ToList(),
            RecommenderMeta = BuildTransparencyMeta(personaTypes),
            GeneralTips = BuildGeneralTips(weather),
            ForeignVisitorTips = profile.NationalityType == TravelerNationalityTypes.Foreigner
                ? BuildForeignVisitorTips(allowedCities)
                : new List<string>(),
            ElderlyCompanionTips = profile.HasElderly
                ? culturalFactResults.Select(f => f.Fact).Where(f => f.Contains("elderly", StringComparison.OrdinalIgnoreCase) || f.Contains("cao tuổi", StringComparison.OrdinalIgnoreCase) || f.Contains("morning", StringComparison.OrdinalIgnoreCase)).Take(4).ToList()
                : new List<string>(),
            ChildrenCompanionTips = profile.HasChildren
                ? new List<string> { _text.TipChildren1, _text.TipChildren2 }
                : new List<string>(),
            Summary = BuildSummary(exactTours, nearbyTours, weather, personas.Count, scheduleAvailability)
        };
    }

    private RecommenderTransparencyDTO BuildTransparencyMeta(IReadOnlyList<string> personaTypes) => new()
    {
        ModelVersion = ScoringModelSpec.ModelVersion,
        ModelFamily = ScoringModelSpec.ModelFamily,
        FairnessAlpha = _settings.FairnessAlpha,
        AggregationFormula = $"FairnessScore = {_settings.FairnessAlpha:0.00} × min(persona_scores) + {1 - _settings.FairnessAlpha:0.00} × mean(persona_scores)",
        PersonaTypesUsed = personaTypes.ToList(),
        KnowledgeSources = _culturalKnowledge.GetRegisteredSources().Select(s => new KnowledgeSourceDTO
        {
            Name = s.Name,
            Url = s.Url,
            Authority = s.Authority
        }).ToList(),
        DimensionWeights = new Dictionary<string, float>
        {
            ["interest_semantic"] = ScoringModelSpec.DimensionWeights.InterestSemantic,
            ["location"] = ScoringModelSpec.DimensionWeights.Location,
            ["budget"] = ScoringModelSpec.DimensionWeights.Budget,
            ["schedule"] = ScoringModelSpec.DimensionWeights.Schedule,
            ["weather"] = ScoringModelSpec.DimensionWeights.Weather,
            ["accessibility"] = ScoringModelSpec.DimensionWeights.Accessibility,
            ["cultural_fit"] = ScoringModelSpec.DimensionWeights.CulturalFit
        }
    };

    private static void ValidateProfile(TourPreferenceQuestionnaireDTO profile)
    {
        if (profile.PreferredEndDate.HasValue && profile.PreferredEndDate.Value.Date < profile.PreferredStartDate.Date)
        {
            throw new InvalidOperationException("PreferredEndDate cannot be before PreferredStartDate.");
        }
    }

    private TourRecommendationItemDTO MapTour(
        TourCatalogItem tour,
        TourScoringResult scoring,
        TourPreferenceQuestionnaireDTO profile,
        DateTime windowStart,
        DateTime windowEnd)
    {
        var publicId = CatalogTourIds.ResolveBaseTourId(tour.Id);
        var display = _catalogStore.Tours.FirstOrDefault(t => t.Id == publicId) ?? tour;
        var matchesDates = ScheduleAvailabilityHelper.IsInPreferredWindow(display.NextDeparture, windowStart, windowEnd);
        var reasons = EnsureScheduleMatchReason(scoring.MatchReasons, matchesDates);
        var scheduleNote = BuildScheduleNote(display.NextDeparture, windowStart, windowEnd, matchesDates);
        var dimensionExplanations = CustomerScoreExplanationBuilder.Build(
            display,
            profile,
            scoring.DimensionScores,
            _dimensionWeights,
            _text);

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
            Score = scoring.FairnessScore,
            MatchReasons = reasons,
            Reason = BuildCustomerReasonSummary(reasons),
            NextDeparture = display.NextDeparture,
            MatchesPreferredDates = matchesDates,
            ScheduleNote = scheduleNote,
            ScoreBreakdown = new TourScoreBreakdownDTO
            {
                FairnessScore = scoring.FairnessScore,
                MinPersonaScore = scoring.MinPersonaScore,
                MeanPersonaScore = scoring.MeanPersonaScore,
                PersonaScores = scoring.PersonaScores,
                DimensionScores = scoring.DimensionScores,
                DimensionExplanations = dimensionExplanations,
                EnvyGap = scoring.EnvyGap,
                DissatisfactionVariance = scoring.DissatisfactionVariance,
                AggregationFormula = ScoringModelSpec.FormalDefinitions.CafhrUtility,
                OverallExplanation = BuildOverallScoreExplanation(scoring)
            }
        };
    }

    private string BuildOverallScoreExplanation(TourScoringResult scoring)
    {
        var percent = $"{Math.Round(Math.Clamp(scoring.FairnessScore, 0f, 1f) * 100)}%";
        var alpha = _settings.FairnessAlpha;

        return _text.IsVietnamese
            ? $"Độ khớp tổng {percent} được tính từ 7 tiêu chí (sở thích, điểm đến, ngân sách, lịch, thời tiết, độ dễ đi, văn hóa), cân bằng sở thích của mọi người trong nhóm (hệ số công bằng {alpha:0.00})."
            : $"Overall match {percent} combines 7 factors (interests, destination, budget, schedule, weather, ease, culture), balancing everyone in your travel party (fairness weight {alpha:0.00}).";
    }

    private List<TourismInsightDTO> MapCulturalInsights(IReadOnlyList<CulturalFactResult> facts) =>
        facts.Select((f, i) => new TourismInsightDTO
        {
            Id = i + 1,
            Name = ResolveInsightTitle(f),
            Type = "Knowledge",
            Description = f.Fact,
            City = f.City,
            SourceName = f.SourceName,
            SourceUrl = f.SourceUrl,
            AuthorityLevel = f.AuthorityLevel,
            KnowledgeProvider = f.Provider,
            RelevanceScore = 1f
        }).ToList();

    private string ResolveInsightTitle(CulturalFactResult f)
    {
        if (!string.IsNullOrWhiteSpace(f.City))
        {
            return _text.IsVietnamese
                ? $"Mẹo khi đến {f.City}"
                : $"Tips for {f.City}";
        }

        return _text.IsVietnamese ? "Gợi ý địa phương" : "Local travel tip";
    }

    private static CulturalFactDTO MapFactDto(CulturalFactResult f) => new()
    {
        Fact = f.Fact,
        SourceName = f.SourceName,
        SourceUrl = f.SourceUrl,
        AuthorityLevel = f.AuthorityLevel,
        Provider = f.Provider,
        City = f.City
    };

    private List<string> BuildGeneralTips(WeatherAdviceDTO? weather)
    {
        var tips = new List<string>();
        if (weather != null)
        {
            tips.Add(weather.Summary);
            tips.Add(weather.ImpactOnTours);
        }

        tips.Add(_text.TipFairnessModel);
        return tips;
    }

    private string BuildScheduleNote(
        DateTime? departure,
        DateTime windowStart,
        DateTime windowEnd,
        bool matchesDates)
    {
        if (!departure.HasValue)
        {
            return _text.ScheduleUnknownDeparture;
        }

        if (matchesDates)
        {
            return _text.ScheduleExactMatch(departure.Value);
        }

        var daysOutside = ScheduleAvailabilityHelper.DaysOutsideWindow(departure, windowStart, windowEnd);
        if (daysOutside > ScheduleAvailabilityHelper.NearbyWindowDays)
        {
            return departure.Value.Date < windowStart.Date
                ? _text.ScheduleExtendedBefore(departure.Value, daysOutside)
                : _text.ScheduleExtendedAfter(departure.Value, daysOutside);
        }

        return departure.Value.Date < windowStart.Date
            ? _text.ScheduleNearbyBefore(departure.Value, daysOutside)
            : _text.ScheduleNearbyAfter(departure.Value, daysOutside);
    }

    private string BuildSummary(
        List<TourRecommendationItemDTO> exactTours,
        List<TourRecommendationItemDTO> nearbyTours,
        WeatherAdviceDTO? weather,
        int personaCount,
        ScheduleAvailabilityDTO schedule)
    {
        var weatherNote = weather != null
            ? (_text.IsVietnamese
                ? $" Thời tiết tại {weather.City} đã được tính vào gợi ý."
                : $" Weather in {weather.City} was considered.")
            : null;

        var totalShown = exactTours.Count + nearbyTours.Count;
        if (totalShown > 0)
        {
            var topScore = exactTours.Count > 0
                ? exactTours[0].Score.ToString("P0")
                : nearbyTours[0].Score.ToString("P0");
            return _text.SummaryFound(personaCount, totalShown, topScore, weatherNote);
        }

        return _text.SummaryNoTours + (weatherNote ?? "");
    }

    private static string? InferCityFromInterests(List<string> interests) =>
        interests.Contains("river", StringComparer.OrdinalIgnoreCase) ? "Can Tho" : null;

    private static TourPreferenceQuestionnaireDTO WithRankingPool(
        TourPreferenceQuestionnaireDTO profile,
        int poolSize) => new()
    {
        CompanionType = profile.CompanionType,
        PreferredStartDate = profile.PreferredStartDate,
        PreferredEndDate = profile.PreferredEndDate,
        MaxBudgetPerPerson = profile.MaxBudgetPerPerson,
        HasElderly = profile.HasElderly,
        HasChildren = profile.HasChildren,
        ChildrenCount = profile.ChildrenCount,
        ElderlyCount = profile.ElderlyCount,
        TravelInterests = profile.TravelInterests,
        NationalityType = profile.NationalityType,
        PreferredCity = profile.PreferredCity,
        PreferredCountry = profile.PreferredCountry,
        Top = poolSize,
        SessionId = profile.SessionId
    };

    private (List<TourRecommendationItemDTO> Exact, List<TourRecommendationItemDTO> Alternate, ScheduleAvailabilityDTO Schedule)
        BuildRecommendationLists(
            List<TourRecommendationItemDTO> mapped,
            int targetTop,
            DateTime windowStart,
            DateTime windowEnd)
    {
        var usedIds = new HashSet<int>();
        var exact = mapped
            .Where(t => t.MatchesPreferredDates)
            .Take(targetTop)
            .ToList();
        foreach (var tour in exact)
        {
            usedIds.Add(tour.TourId);
        }

        var alternate = new List<TourRecommendationItemDTO>();
        var remaining = targetTop - exact.Count;
        if (remaining > 0)
        {
            alternate.AddRange(TakeScheduleCandidates(
                mapped, usedIds, remaining, windowStart, windowEnd,
                minDaysOutside: 1,
                maxDaysOutside: ScheduleAvailabilityHelper.NearbyWindowDays));
            remaining = targetTop - exact.Count - alternate.Count;
        }

        if (remaining > 0)
        {
            alternate.AddRange(TakeScheduleCandidates(
                mapped, usedIds, remaining, windowStart, windowEnd,
                minDaysOutside: ScheduleAvailabilityHelper.NearbyWindowDays + 1,
                maxDaysOutside: ScheduleAvailabilityHelper.ExtendedWindowDays));
            remaining = targetTop - exact.Count - alternate.Count;
        }

        if (remaining > 0)
        {
            var fallback = mapped
                .Where(t => !usedIds.Contains(t.TourId))
                .OrderByDescending(t => t.Score)
                .Take(remaining)
                .ToList();
            alternate.AddRange(fallback);
            foreach (var tour in fallback)
            {
                usedIds.Add(tour.TourId);
            }
        }

        var schedule = new ScheduleAvailabilityDTO
        {
            HasToursInPreferredWindow = exact.Count > 0,
            PreferredStartDate = windowStart.ToString("yyyy-MM-dd"),
            PreferredEndDate = windowEnd.ToString("yyyy-MM-dd"),
            CustomerMessage = BuildScheduleCustomerMessage(exact.Count, alternate.Count)
        };

        return (exact, alternate, schedule);
    }

    private static List<TourRecommendationItemDTO> TakeScheduleCandidates(
        List<TourRecommendationItemDTO> mapped,
        HashSet<int> usedIds,
        int take,
        DateTime windowStart,
        DateTime windowEnd,
        int minDaysOutside,
        int maxDaysOutside)
    {
        if (take <= 0)
        {
            return new List<TourRecommendationItemDTO>();
        }

        var picked = mapped
            .Where(t => !usedIds.Contains(t.TourId))
            .Where(t => t.NextDeparture.HasValue && !t.MatchesPreferredDates)
            .Select(t => new
            {
                Tour = t,
                DaysOutside = ScheduleAvailabilityHelper.DaysOutsideWindow(t.NextDeparture, windowStart, windowEnd)
            })
            .Where(x => x.DaysOutside >= minDaysOutside && x.DaysOutside <= maxDaysOutside)
            .OrderBy(x => x.DaysOutside)
            .ThenByDescending(x => x.Tour.Score)
            .Take(take)
            .Select(x => x.Tour)
            .ToList();

        foreach (var tour in picked)
        {
            usedIds.Add(tour.TourId);
        }

        return picked;
    }

    private string BuildScheduleCustomerMessage(int exactCount, int alternateCount)
    {
        if (exactCount > 0 && alternateCount > 0)
        {
            return _text.SchedulePartialExactAndNearby(exactCount, alternateCount);
        }

        if (exactCount > 0)
        {
            return _text.ScheduleHasExact(exactCount);
        }

        if (alternateCount > 0)
        {
            return _text.ScheduleNoExactButNearby;
        }

        return _text.SummaryNoTours;
    }

    private static List<string> ResolveAllowedCities(
        IReadOnlyList<TourRecommendationItemDTO> exactTours,
        IReadOnlyList<TourRecommendationItemDTO> alternateTours,
        TourPreferenceQuestionnaireDTO profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.PreferredCity))
        {
            return new List<string> { profile.PreferredCity.Trim() };
        }

        return exactTours
            .Concat(alternateTours)
            .Select(t => t.City)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private List<string> BuildForeignVisitorTips(IReadOnlyList<string> allowedCities)
    {
        var tips = new List<string> { _text.ForeignVisitorGeneralHeader };
        tips.AddRange(_text.ForeignVisitorGeneralDosAndDonts);

        foreach (var city in allowedCities.Take(2))
        {
            var displayCity = _knowledgeLocalizer.LocalizeCityDisplay(city);
            var cityNotes = _culturalKnowledge.GetForeignVisitorNotesForCity(city);
            if (cityNotes.Count == 0)
            {
                continue;
            }

            tips.Add(_text.ForeignVisitorDestinationHeader(displayCity));
            tips.AddRange(cityNotes);
        }

        return tips;
    }

    private async Task<IReadOnlyList<CulturalFactResult>> GetFactsForRecommendedToursAsync(
        IReadOnlyList<string> allowedCities,
        TourPreferenceQuestionnaireDTO profile,
        CancellationToken cancellationToken)
    {
        if (allowedCities.Count == 0)
        {
            return Array.Empty<CulturalFactResult>();
        }

        var merged = new List<CulturalFactResult>();
        foreach (var city in allowedCities)
        {
            var facts = await _culturalKnowledge.GetFactsAsync(
                city,
                forForeignVisitor: false,
                profile.HasElderly,
                profile.HasChildren,
                profile.TravelInterests,
                cancellationToken);

            merged.AddRange(facts.Where(f =>
                string.IsNullOrWhiteSpace(f.City) ||
                allowedCities.Any(c => VietnameseTextNormalizer.CityEquals(f.City, c))));
        }

        return merged
            .GroupBy(f => f.Fact, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(12)
            .ToList();
    }

    private List<string> EnsureScheduleMatchReason(IEnumerable<string> reasons, bool matchesDates)
    {
        var list = reasons.Distinct().ToList();
        if (!matchesDates)
        {
            return list.Take(8).ToList();
        }

        var scheduleReason = _text.ReasonScheduleFit;
        if (list.All(r => !string.Equals(r, scheduleReason, StringComparison.OrdinalIgnoreCase)
                          && !r.Contains("lịch khởi hành", StringComparison.OrdinalIgnoreCase)
                          && !r.Contains("departure date", StringComparison.OrdinalIgnoreCase)))
        {
            list.Insert(0, scheduleReason);
        }

        return list.Take(8).ToList();
    }

    private static string BuildCustomerReasonSummary(IReadOnlyList<string> reasons)
    {
        if (reasons.Count == 0) return string.Empty;

        static bool IsCaution(string r) =>
            r.Contains("Cân nhắc", StringComparison.OrdinalIgnoreCase)
            || r.Contains("hơi mệt", StringComparison.OrdinalIgnoreCase)
            || r.Contains("Consider carefully", StringComparison.OrdinalIgnoreCase)
            || r.Contains("tiring", StringComparison.OrdinalIgnoreCase)
            || r.Contains("Worth a closer look", StringComparison.OrdinalIgnoreCase);

        var highlights = reasons.Where(r => !IsCaution(r)).Distinct().Take(2).ToList();
        if (highlights.Count == 0)
        {
            highlights = reasons.Distinct().Take(1).ToList();
        }

        return string.Join(" ", highlights);
    }
}

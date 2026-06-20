using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models.Catalog;
using AIAPI.Models.Knowledge;
using AIAPI.Recommender;
using AIAPI.Services;
using AIAPI.Settings;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;

    public PersonalizedTourRecommendationService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        IWeatherService weatherService,
        ICulturalKnowledgeService culturalKnowledge,
        TourRanker tourRanker,
        IOptions<RecommenderSettings> settings,
        IAiLocalizedCopy text,
        IKnowledgeLocalizationService knowledgeLocalizer,
        IDimensionWeightProvider dimensionWeights,
        IMemoryCache cache)
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
        _cache = cache;
    }

    public StandardQuestionnaireDTO GetStandardQuestionnaire() => new()
    {
        Version = "2.1",
        Questions =
        [
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
                FieldKey = "adultCount",
                Label = "Bao nhiêu người lớn?",
                InputType = "number",
                Required = true
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "childrenCount",
                Label = "Có \"búp măng non\" đi cùng không? (trẻ em)",
                InputType = "number",
                Required = true
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "elderlyCount",
                Label = "Có \"bậc thầy dưỡng sinh\" đi cùng không? (người cao tuổi)",
                InputType = "number",
                Required = true
            },
            new QuestionnaireFieldDTO
            {
                FieldKey = "travelPace",
                Label = "Pace chuyến đi (Nhịp độ)",
                InputType = "single_select",
                Required = true,
                Options =
                [
                    new() { Value = TravelPaceTypes.Relaxed, Label = "Chill chill lướt sóng 🍃" },
                    new() { Value = TravelPaceTypes.Moderate, Label = "Balance (Nghỉ + Chơi) ⚖️" },
                    new() { Value = TravelPaceTypes.Packed, Label = "Bào tour không bỏ sót! 🔥" }
                ]
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
                InputType = "single_select",
                Required = false,
                Hint = _text.QuestionPreferredCityHint,
                Options =
                [
                    new() { Value = "", Label = "Bất kỳ đâu (Surprise me!)" },
                    new() { Value = "Phu Quoc", Label = "Phú Quốc" },
                    new() { Value = "Da Nang", Label = "Đà Nẵng" },
                    new() { Value = "Da Lat", Label = "Đà Lạt" },
                    new() { Value = "Nha Trang", Label = "Nha Trang" },
                    new() { Value = "Ha Noi", Label = "Hà Nội" },
                    new() { Value = "Ho Chi Minh", Label = "TP. HCM" },
                    new() { Value = "Can Tho", Label = "Cần Thơ" }
                ]
            }
        ]
    };

    public ScoringModelDocumentationDTO GetScoringDocumentation() => new()
    {
        Specification = BuildTransparencyMeta(Array.Empty<string>()),
        MethodologySummary =
            ScoringModelSpec.MethodologySummary + " " +
            "Multi-source knowledge augmentation uses UNESCO-cited corpus rows, ContentAPI, Wikidata CC0, and Open-Meteo. " +
            "Baselines available via GET /api/ai/evaluation/baselines and POST /api/ai/evaluation/run.",
        PaperTitleSuggestion = ScoringModelSpec.PaperTitleSuggestion,
        FormalDefinitions = new Dictionary<string, string>
        {
            ["persona_utility"] = ScoringModelSpec.FormalDefinitions.PersonaUtility,
            ["fcahr_utility"] = ScoringModelSpec.FormalDefinitions.CafhrUtility,
            ["mgrs_fair_reranking"] = ScoringModelSpec.FormalDefinitions.MgrsFairReranking,
            ["min_persona_penalty"] = ScoringModelSpec.FormalDefinitions.MinPersonaPenalty,
            ["dissatisfaction_variance"] = ScoringModelSpec.FormalDefinitions.DissatisfactionVariance,
            ["envy_gap"] = ScoringModelSpec.FormalDefinitions.EnvyGap,
            ["mean_baseline"] = ScoringModelSpec.FormalDefinitions.MeanBaseline,
            ["least_misery_baseline"] = ScoringModelSpec.FormalDefinitions.LeastMiseryBaseline,
            ["borda_baseline"] = ScoringModelSpec.FormalDefinitions.BordaBaseline,
            ["evaluation_protocol"] = ScoringModelSpec.EvaluationProtocol.ProfileGeneration
        },
        AcademicReferences = MapAcademicReferences(),
        Baselines = AggregationStrategies.GetAll()
            .Select(b => new BaselineInfoDTO
            {
                Key = b.Key,
                Name = b.Name,
                Description = b.Description,
                IsProposed = b.Key == ScoringModelSpec.ProductionStrategyKey
            })
            .ToList()
    };

    public async Task<PersonalizedRecommendationResponseDTO> RecommendFromProfileAsync(
        TourPreferenceQuestionnaireDTO profile,
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
        var semanticCacheKey = $"semantic_{interestQuery.GetHashCode()}";
        if (!_cache.TryGetValue(semanticCacheKey, out Dictionary<int, float>? semanticScores) || semanticScores == null)
        {
            semanticScores = _modelRegistry.ComputeTourSemanticScores(interestQuery)
                .ToDictionary(x => x.Key, x => x.Value);
            _cache.Set(semanticCacheKey, semanticScores, TimeSpan.FromMinutes(15));
        }

        var activeProfile = profile;
        var isRelaxed = false;
        List<TourRecommendationItemDTO> exactTours = new();
        List<TourRecommendationItemDTO> nearbyTours = new();
        ScheduleAvailabilityDTO scheduleAvailability = new();

        var windowStart = profile.PreferredStartDate.Date;
        var windowEnd = ScheduleAvailabilityHelper.ResolveWindowEnd(profile.PreferredStartDate, profile.PreferredEndDate);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            var profileMatchScores = _modelRegistry.ComputeProfileMatchScores(activeProfile);
            var hybridRetrievalScores = BlendRetrievalScores(semanticScores, profileMatchScores);

            var rankingPool = Math.Min(
                _catalogStore.Tours.Count,
                Math.Max(activeProfile.Top * 5, 30));

            var ranked = _tourRanker.RankTours(
                _catalogStore.Tours.ToList(),
                activeProfile,
                hybridRetrievalScores,
                weather,
                ScoringModelSpec.ProductionStrategyKey);

            var rankedDistinct = ranked
                .GroupBy(x => CatalogTourIds.ResolveBaseTourId(x.Tour.Id))
                .Select(g => g.OrderByDescending(x => x.Scoring.FairnessScore).First())
                .ToList();

            var mapped = rankedDistinct
                .Select(x => 
                {
                    var scoringToUse = x.Scoring;
                    if (isRelaxed)
                    {
                        scoringToUse = _tourRanker.ScoreTour(x.Tour, profile, hybridRetrievalScores, weather);
                    }
                    return MapTour(x.Tour, scoringToUse, profile, windowStart, windowEnd);
                })
                .ToList();

            var lists = BuildRecommendationLists(mapped, activeProfile.Top, windowStart, windowEnd);
            exactTours = lists.Exact;
            nearbyTours = lists.Alternate;
            scheduleAvailability = lists.Schedule;

            if (exactTours.Count > 0 || nearbyTours.Count > 0)
            {
                break;
            }

            if (attempt == 0)
            {
                isRelaxed = true;
                activeProfile = CloneProfile(activeProfile);
                activeProfile.PreferredCity = null;
                if (activeProfile.MaxBudgetPerPerson.HasValue)
                {
                    activeProfile.MaxBudgetPerPerson = (long)(activeProfile.MaxBudgetPerPerson.Value * 1.5f);
                }
            }
        }

        var allowedCities = ResolveAllowedCities(exactTours, nearbyTours, profile);

        var culturalFactResults = await GetFactsForRecommendedToursAsync(
            allowedCities,
            profile,
            cancellationToken);

        var personaTypes = personas.Select(p => p.PersonaType).ToList();

        return new PersonalizedRecommendationResponseDTO
        {
            SessionId = sessionId,
            AppliedProfile = activeProfile,
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
            ElderlyCompanionTips = profile.ElderlyCount > 0
                ? culturalFactResults.Select(f => f.Fact).Where(f => f.Contains("elderly", StringComparison.OrdinalIgnoreCase) || f.Contains("cao tuổi", StringComparison.OrdinalIgnoreCase) || f.Contains("morning", StringComparison.OrdinalIgnoreCase)).Take(4).ToList()
                : new List<string>(),
            ChildrenCompanionTips = profile.ChildrenCount > 0
                ? new List<string> { _text.TipChildren1, _text.TipChildren2 }
                : new List<string>(),
            Summary = BuildSummary(exactTours, nearbyTours, weather, personas.Count, scheduleAvailability, isRelaxed)
        };
    }

    private TourPreferenceQuestionnaireDTO CloneProfile(TourPreferenceQuestionnaireDTO p) => new()
    {
        CompanionType = p.CompanionType,
        PreferredStartDate = p.PreferredStartDate,
        PreferredEndDate = p.PreferredEndDate,
        AdultCount = p.AdultCount,
        ElderlyCount = p.ElderlyCount,
        ChildrenCount = p.ChildrenCount,
        TravelPace = p.TravelPace,
        TravelInterests = p.TravelInterests.ToList(),
        PreferredCity = p.PreferredCity,
        PreferredCountry = p.PreferredCountry,
        MaxBudgetPerPerson = p.MaxBudgetPerPerson,
        Top = p.Top,
        NationalityType = p.NationalityType,
        SessionId = p.SessionId
    };

    private RecommenderTransparencyDTO BuildTransparencyMeta(IReadOnlyList<string> personaTypes) => new()
    {
        ModelVersion = ScoringModelSpec.ModelVersion,
        ModelFamily = ScoringModelSpec.ModelFamily,
        MethodologySummary = ScoringModelSpec.MethodologySummary,
        FairnessAlpha = _settings.FairnessAlpha,
        AggregationFormula =
            $"SeedScore = {_settings.FairnessAlpha:0.00} × min(persona_scores) + {1 - _settings.FairnessAlpha:0.00} × mean(persona_scores); " +
            "MGRS-Fair re-ranks the top-N list to improve the least-satisfied persona.",
        PersonaTypesUsed = personaTypes.ToList(),
        KnowledgeSources = _culturalKnowledge.GetRegisteredSources().Select(s => new KnowledgeSourceDTO
        {
            Name = s.Name,
            Url = s.Url,
            Authority = s.Authority
        }).ToList(),
        AcademicReferences = MapAcademicReferences(),
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

    private static List<AcademicReferenceDTO> MapAcademicReferences() =>
        ScoringModelSpec.AcademicReferences.Select(r => new AcademicReferenceDTO
        {
            Key = r.Key,
            Title = r.Title,
            Authors = r.Authors,
            Venue = r.Venue,
            Year = r.Year,
            Url = r.Url,
            Doi = r.Doi,
            UsedFor = r.UsedFor
        }).ToList();

    private static Dictionary<int, float> BlendRetrievalScores(
        IReadOnlyDictionary<int, float> semanticScores,
        IReadOnlyDictionary<int, float> profileMatchScores)
    {
        if (profileMatchScores.Count == 0)
        {
            return semanticScores.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        var ids = semanticScores.Keys
            .Concat(profileMatchScores.Keys)
            .Distinct()
            .ToList();

        return ids.ToDictionary(
            id => id,
            id =>
            {
                var semantic = semanticScores.GetValueOrDefault(id, 0f);
                var pm = profileMatchScores.GetValueOrDefault(id, 0f);
                return Math.Clamp(semantic * 0.55f + pm * 0.45f, 0f, 1f);
            });
    }

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
                AggregationFormula = ScoringModelSpec.FormalDefinitions.MgrsFairReranking,
                OverallExplanation = BuildOverallScoreExplanation(scoring)
            }
        };
    }

    private string BuildOverallScoreExplanation(TourScoringResult scoring)
    {
        var percent = $"{Math.Round(Math.Clamp(scoring.FairnessScore, 0f, 1f) * 100)}%";
        var alpha = _settings.FairnessAlpha;

        return _text.IsVietnamese
            ? $"Độ khớp tổng {percent} được tính từ 7 tiêu chí, sau đó mô hình MGRS-Fair sắp xếp lại danh sách để giảm rủi ro bỏ quên thành viên có mức phù hợp thấp nhất (hệ số công bằng {alpha:0.00})."
            : $"Overall match {percent} combines 7 factors, then MGRS-Fair re-ranks the list to reduce the risk of leaving the least-satisfied traveler behind (fairness weight {alpha:0.00}).";
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
        IReadOnlyList<TourRecommendationItemDTO> exactTours,
        IReadOnlyList<TourRecommendationItemDTO> nearbyTours,
        WeatherAdviceDTO? weather,
        int personaCount,
        ScheduleAvailabilityDTO scheduleAvailability,
        bool isRelaxed)
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
                
            return isRelaxed
                ? _text.SummaryFoundRelaxed(personaCount, totalShown, topScore, weatherNote)
                : _text.SummaryFound(personaCount, totalShown, topScore, weatherNote);
        }

        return _text.SummaryNoTours + (weatherNote ?? "");
    }

    private static string InferCityFromInterests(List<string> interests)
    {
        if (interests.Contains("beach", StringComparer.OrdinalIgnoreCase)) return "Nha Trang";
        if (interests.Contains("nature", StringComparer.OrdinalIgnoreCase)) return "Da Lat";
        if (interests.Contains("culture", StringComparer.OrdinalIgnoreCase)) return "Hue";
        if (interests.Contains("food", StringComparer.OrdinalIgnoreCase)) return "Ho Chi Minh";
        if (interests.Contains("river", StringComparer.OrdinalIgnoreCase)) return "Can Tho";
        return "Da Nang";
    }

    private static TourPreferenceQuestionnaireDTO WithRankingPool(
        TourPreferenceQuestionnaireDTO profile,
        int poolSize) => new()
    {
        CompanionType = profile.CompanionType,
        PreferredStartDate = profile.PreferredStartDate,
        PreferredEndDate = profile.PreferredEndDate,
        MaxBudgetPerPerson = profile.MaxBudgetPerPerson,
        AdultCount = profile.AdultCount,
        TravelPace = profile.TravelPace,
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
            .Take(8)
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
                profile.ElderlyCount > 0,
                profile.ChildrenCount > 0,
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

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

    public PersonalizedTourRecommendationService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        IWeatherService weatherService,
        ICulturalKnowledgeService culturalKnowledge,
        TourRanker tourRanker,
        IOptions<RecommenderSettings> settings,
        IAiLocalizedCopy text)
    {
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _weatherService = weatherService;
        _culturalKnowledge = culturalKnowledge;
        _tourRanker = tourRanker;
        _settings = settings.Value;
        _text = text;
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

        var interestQuery = string.Join(" ", profile.TravelInterests);
        var semanticScores = _modelRegistry.SearchTours(interestQuery, _catalogStore.Tours.Count)
            .ToDictionary(x => x.TourId, x => x.Score);

        var ranked = _tourRanker.RankTours(
            _catalogStore.Tours.ToList(),
            profile,
            semanticScores,
            weather,
            AggregationStrategies.CafhrFair);

        var scoredTours = ranked
            .GroupBy(x => CatalogTourIds.ResolveBaseTourId(x.Tour.Id))
            .Select(g => g.OrderByDescending(x => x.Scoring.FairnessScore).First())
            .Select(x => MapTour(x.Tour, x.Scoring))
            .ToList();

        var culturalFacts = await _culturalKnowledge.GetFactsAsync(
            profile.PreferredCity ?? weatherCity,
            profile.NationalityType == TravelerNationalityTypes.Foreigner,
            profile.HasElderly,
            profile.HasChildren,
            profile.TravelInterests,
            cancellationToken);

        var personaTypes = personas.Select(p => p.PersonaType).ToList();

        return new PersonalizedRecommendationResponseDTO
        {
            SessionId = sessionId,
            AppliedProfile = profile,
            WeatherAdvice = weather,
            RecommendedTours = scoredTours,
            RelatedInsights = MapCulturalInsights(culturalFacts),
            CulturalFacts = culturalFacts.Select(MapFactDto).ToList(),
            RecommenderMeta = BuildTransparencyMeta(personaTypes),
            GeneralTips = BuildGeneralTips(weather),
            ForeignVisitorTips = profile.NationalityType == TravelerNationalityTypes.Foreigner
                ? culturalFacts.Where(f => f.Provider != ScoringModelSpec.KnowledgeSources.ContentApi)
                    .Select(f => f.Fact).Distinct().Take(6).ToList()
                : new List<string>(),
            ElderlyCompanionTips = profile.HasElderly
                ? culturalFacts.Select(f => f.Fact).Where(f => f.Contains("elderly", StringComparison.OrdinalIgnoreCase) || f.Contains("cao tuổi", StringComparison.OrdinalIgnoreCase) || f.Contains("morning", StringComparison.OrdinalIgnoreCase)).Take(4).ToList()
                : new List<string>(),
            ChildrenCompanionTips = profile.HasChildren
                ? new List<string> { _text.TipChildren1, _text.TipChildren2 }
                : new List<string>(),
            Summary = BuildSummary(scoredTours, weather, personas.Count)
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

    private TourRecommendationItemDTO MapTour(TourCatalogItem tour, TourScoringResult scoring)
    {
        var publicId = CatalogTourIds.ResolveBaseTourId(tour.Id);
        var display = _catalogStore.Tours.FirstOrDefault(t => t.Id == publicId) ?? tour;
        var reasons = scoring.MatchReasons.Distinct().Take(8).ToList();
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
            Reason = string.Join(" ", reasons),
            ScoreBreakdown = new TourScoreBreakdownDTO
            {
                FairnessScore = scoring.FairnessScore,
                MinPersonaScore = scoring.MinPersonaScore,
                MeanPersonaScore = scoring.MeanPersonaScore,
                PersonaScores = scoring.PersonaScores,
                DimensionScores = scoring.DimensionScores,
                EnvyGap = scoring.EnvyGap,
                DissatisfactionVariance = scoring.DissatisfactionVariance,
                AggregationFormula = ScoringModelSpec.FormalDefinitions.CafhrUtility
            }
        };
    }

    private static List<TourismInsightDTO> MapCulturalInsights(IReadOnlyList<CulturalFactResult> facts) =>
        facts.Select((f, i) => new TourismInsightDTO
        {
            Id = i + 1,
            Name = f.City ?? "Cultural insight",
            Type = "Knowledge",
            Description = f.Fact,
            City = f.City,
            SourceName = f.SourceName,
            SourceUrl = f.SourceUrl,
            AuthorityLevel = f.AuthorityLevel,
            KnowledgeProvider = f.Provider,
            RelevanceScore = 1f
        }).ToList();

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

    private string BuildSummary(
        List<TourRecommendationItemDTO> tours,
        WeatherAdviceDTO? weather,
        int personaCount)
    {
        if (tours.Count == 0)
        {
            return _text.SummaryNoTours;
        }

        var weatherNote = weather != null
            ? (_text.IsVietnamese ? $" Thời tiết ({weather.DataSource})." : $" Weather ({weather.DataSource}).")
            : null;
        return _text.SummaryFound(personaCount, tours.Count, tours[0].Score.ToString("P0"), weatherNote);
    }

    private static string? InferCityFromInterests(List<string> interests) =>
        interests.Contains("river", StringComparer.OrdinalIgnoreCase) ? "Can Tho" : null;
}

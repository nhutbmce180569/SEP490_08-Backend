using AIAPI.DTOs;
using AIAPI.Localization;
using AIAPI.Models.Catalog;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Recommender;

public class TourRanker
{
    private readonly TourScoringEngine _scoringEngine;
    private readonly RecommenderSettings _settings;
    private readonly IAiLocalizedCopy _text;

    public TourRanker(
        TourScoringEngine scoringEngine,
        IOptions<RecommenderSettings> settings,
        IAiLocalizedCopy text)
    {
        _scoringEngine = scoringEngine;
        _settings = settings.Value;
        _text = text;
    }

    public TourScoringResult ScoreTour(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float>? semanticScores,
        WeatherAdviceDTO? weather)
    {
        var personas = TravelPartyDecomposer.Decompose(profile, _text);
        var scoring = _scoringEngine.ScoreTour(tour, profile, personas, semanticScores ?? new Dictionary<int, float>(), weather, includeKnowledgeDimensions: true);
        
        // Re-calculate FairnessScore based on ProductionStrategy (CafhrFair)
        scoring.FairnessScore = ComputeAggregateUtility(
            scoring.PersonaScores, 
            ScoringModelSpec.ProductionStrategyKey, 
            _settings.FairnessAlpha, 
            _settings.MinPersonaScoreThreshold);
            
        return scoring;
    }

    public IReadOnlyList<(TourCatalogItem Tour, TourScoringResult Scoring)> RankTours(
        IReadOnlyList<TourCatalogItem> catalog,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float> semanticScores,
        WeatherAdviceDTO? weather,
        string aggregationStrategy,
        float? fairnessAlphaOverride = null,
        IReadOnlyDictionary<int, float>? popularityScores = null)
    {
        if (aggregationStrategy == AggregationStrategies.ContentOnly)
        {
            return RankContentOnly(catalog, profile, semanticScores, weather);
        }

        if (aggregationStrategy == AggregationStrategies.PopularityWeighted)
        {
            return RankPopularityWeighted(catalog, profile, semanticScores, weather, popularityScores);
        }

        var personas = TravelPartyDecomposer.Decompose(profile, _text);
        var includeKnowledge = aggregationStrategy != AggregationStrategies.CafhrNoKnowledge;
        var alpha = fairnessAlphaOverride ?? _settings.FairnessAlpha;

        var scored = catalog
            .Select(tour =>
            {
                var scoring = _scoringEngine.ScoreTour(
                    tour, profile, personas, semanticScores, weather, includeKnowledge);
                return (tour, scoring);
            })
            .Where(x => x.scoring.PassesHardConstraints)
            .ToList();

        if (aggregationStrategy == AggregationStrategies.BordaCount)
        {
            return RankByBorda(scored, personas, profile.Top);
        }

        if (aggregationStrategy == AggregationStrategies.MgrsFair)
        {
            return RankByMgrsFair(scored, personas, profile.Top, alpha, _settings.MinPersonaScoreThreshold);
        }

        var ranked = scored
            .Select(x =>
            {
                x.scoring.FairnessScore = ComputeAggregateUtility(
                    x.scoring.PersonaScores,
                    aggregationStrategy,
                    alpha,
                    _settings.MinPersonaScoreThreshold);
                return x;
            })
            .Where(x => x.scoring.FairnessScore > 0)
            .OrderByDescending(x => x.scoring.FairnessScore)
            .Take(profile.Top)
            .ToList();

        return ranked;
    }

    private IReadOnlyList<(TourCatalogItem Tour, TourScoringResult Scoring)> RankContentOnly(
        IReadOnlyList<TourCatalogItem> catalog,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float> semanticScores,
        WeatherAdviceDTO? weather)
    {
        var personas = TravelPartyDecomposer.Decompose(profile, _text);
        var primaryType = ScoringModelSpec.PersonaTypes.Primary;

        return catalog
            .Select(tour =>
            {
                var scoring = _scoringEngine.ScoreTour(
                    tour, profile, personas, semanticScores, weather, includeKnowledgeDimensions: true);

                if (!scoring.PassesHardConstraints)
                {
                    return (tour, scoring);
                }

                foreach (var key in scoring.PersonaScores.Keys.Where(k => k != primaryType).ToList())
                {
                    scoring.PersonaScores[key] = 0f;
                }

                var primaryScore = scoring.PersonaScores.GetValueOrDefault(primaryType, 0f);
                scoring.FairnessScore = primaryScore;
                scoring.MinPersonaScore = primaryScore;
                scoring.MeanPersonaScore = personas.Count > 0 ? primaryScore / personas.Count : primaryScore;
                return (tour, scoring);
            })
            .Where(x => x.scoring.PassesHardConstraints && x.scoring.FairnessScore > 0)
            .OrderByDescending(x => x.scoring.FairnessScore)
            .Take(profile.Top)
            .ToList();
    }

    private IReadOnlyList<(TourCatalogItem Tour, TourScoringResult Scoring)> RankPopularityWeighted(
        IReadOnlyList<TourCatalogItem> catalog,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float> semanticScores,
        WeatherAdviceDTO? weather,
        IReadOnlyDictionary<int, float>? popularityScores)
    {
        var personas = TravelPartyDecomposer.Decompose(profile, _text);
        var rawPop = catalog.ToDictionary(t => t.Id, t => popularityScores?.GetValueOrDefault(t.Id, 0f) ?? 0f);
        var maxPop = rawPop.Values.DefaultIfEmpty(0f).Max();
        var minPop = rawPop.Values.DefaultIfEmpty(0f).Min();
        var popRange = maxPop - minPop;

        return catalog
            .Select(tour =>
            {
                var scoring = _scoringEngine.ScoreTour(
                    tour, profile, personas, semanticScores, weather, includeKnowledgeDimensions: true);

                if (!scoring.PassesHardConstraints)
                {
                    return (tour, scoring);
                }

                var s1 = scoring.DimensionScores.GetValueOrDefault("interest_semantic", 0f);
                if (s1 <= 0f)
                {
                    s1 = semanticScores.GetValueOrDefault(tour.Id, 0f);
                }

                var normPop = popRange <= 0
                    ? 0f
                    : (rawPop[tour.Id] - minPop) / popRange;
                scoring.FairnessScore = 0.6f * normPop + 0.4f * s1;
                scoring.DimensionScores["popularity"] = normPop;
                scoring.MinPersonaScore = scoring.PersonaScores.Values.DefaultIfEmpty(0f).Min();
                return (tour, scoring);
            })
            .Where(x => x.scoring.PassesHardConstraints && x.scoring.FairnessScore > 0)
            .OrderByDescending(x => x.scoring.FairnessScore)
            .Take(profile.Top)
            .ToList();
    }

    private static float ComputeAggregateUtility(
        IReadOnlyDictionary<string, float> personaScores,
        string strategy,
        float alpha,
        float minThreshold)
    {
        float utility = strategy switch
        {
            AggregationStrategies.LeastMisery => FairnessFormalization.LeastMiseryUtility(personaScores),
            AggregationStrategies.MeanUtility => FairnessFormalization.MeanUtility(personaScores),
            _ => FairnessFormalization.CafhrUtility(personaScores, alpha)
        };

        if (strategy is AggregationStrategies.CafhrFair or AggregationStrategies.CafhrNoKnowledge)
        {
            var min = personaScores.Count > 0 ? personaScores.Values.Min() : 0f;
            utility = FairnessFormalization.ApplyMinPersonaPenalty(utility, min, minThreshold);
        }

        return utility;
    }

    private static List<(TourCatalogItem Tour, TourScoringResult Scoring)> RankByBorda(
        List<(TourCatalogItem tour, TourScoringResult scoring)> scored,
        IReadOnlyList<TravelPersona> personas,
        int top)
    {
        var bordaTotals = scored.ToDictionary(x => x.tour.Id, _ => 0f);

        foreach (var persona in personas)
        {
            var ranked = scored
                .Select(x =>
                {
                    var personaScore = x.scoring.PersonaScores.GetValueOrDefault(persona.PersonaType, 0f);
                    return (x.tour, personaScore);
                })
                .OrderByDescending(x => x.personaScore)
                .ToList();

            for (var rank = 0; rank < ranked.Count; rank++)
            {
                bordaTotals[ranked[rank].tour.Id] += ranked.Count - rank;
            }
        }

        return scored
            .Select(x =>
            {
                x.scoring.FairnessScore = bordaTotals[x.tour.Id] / Math.Max(personas.Count * scored.Count, 1);
                return x;
            })
            .OrderByDescending(x => x.scoring.FairnessScore)
            .Take(top)
            .ToList();
    }

    private static List<(TourCatalogItem Tour, TourScoringResult Scoring)> RankByMgrsFair(
        List<(TourCatalogItem tour, TourScoringResult scoring)> scored,
        IReadOnlyList<TravelPersona> personas,
        int top,
        float alpha,
        float minThreshold)
    {
        if (scored.Count == 0)
        {
            return scored;
        }

        var pool = scored
            .Select(x =>
            {
                x.scoring.FairnessScore = ComputeAggregateUtility(
                    x.scoring.PersonaScores, AggregationStrategies.CafhrFair, alpha, minThreshold);
                return x;
            })
            .OrderByDescending(x => x.scoring.FairnessScore)
            .ToList();

        var selected = pool.Take(top).ToList();
        var selectedIds = selected.Select(x => x.tour.Id).ToHashSet();
        var candidates = pool.Skip(top).ToList();

        for (var iter = 0; iter < 50; iter++)
        {
            var improved = false;
            for (var i = 0; i < selected.Count; i++)
            {
                var currentMin = MinPersonaOfList(selected);
                foreach (var candidate in candidates)
                {
                    var trial = selected.ToList();
                    trial[i] = candidate;
                    var trialMin = MinPersonaOfList(trial);
                    if (trialMin > currentMin + 1e-5f)
                    {
                        selected = trial;
                        selectedIds = selected.Select(x => x.tour.Id).ToHashSet();
                        candidates = pool.Where(x => !selectedIds.Contains(x.tour.Id)).ToList();
                        improved = true;
                        break;
                    }
                }

                if (improved)
                {
                    break;
                }
            }

            if (!improved)
            {
                break;
            }
        }

        return selected
            .Select(x =>
            {
                x.scoring.FairnessScore = ComputeAggregateUtility(
                    x.scoring.PersonaScores, AggregationStrategies.CafhrFair, alpha, minThreshold);
                return x;
            })
            .OrderByDescending(x => x.scoring.FairnessScore)
            .ToList();
    }

    private static float MinPersonaOfList(IReadOnlyList<(TourCatalogItem tour, TourScoringResult scoring)> items) =>
        items.Count == 0
            ? 0f
            : items.Min(x => x.scoring.PersonaScores.Values.DefaultIfEmpty(0f).Min());
}

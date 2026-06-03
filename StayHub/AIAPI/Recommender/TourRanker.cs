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

    public IReadOnlyList<(TourCatalogItem Tour, TourScoringResult Scoring)> RankTours(
        IReadOnlyList<TourCatalogItem> catalog,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<int, float> semanticScores,
        WeatherAdviceDTO? weather,
        string aggregationStrategy,
        float? fairnessAlphaOverride = null)
    {
        if (aggregationStrategy == AggregationStrategies.ContentOnly)
        {
            return catalog
                .Select(t => (
                    t,
                    new TourScoringResult
                    {
                        PassesHardConstraints = true,
                        FairnessScore = semanticScores.GetValueOrDefault(t.Id, 0f),
                        DimensionScores = new Dictionary<string, float> { ["interest_semantic"] = semanticScores.GetValueOrDefault(t.Id, 0f) }
                    }))
                .OrderByDescending(x => x.Item2.FairnessScore)
                .Take(profile.Top)
                .ToList();
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

        var ranked = scored
            .Select(x =>
            {
                x.scoring.FairnessScore = ComputeAggregateUtility(
                    x.scoring.PersonaScores, aggregationStrategy, alpha, _settings.MinPersonaScoreThreshold);
                return x;
            })
            .Where(x => x.scoring.FairnessScore > 0)
            .OrderByDescending(x => x.scoring.FairnessScore)
            .Take(profile.Top)
            .ToList();

        return ranked;
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
}

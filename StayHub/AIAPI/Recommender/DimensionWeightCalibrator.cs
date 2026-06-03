using AIAPI.DTOs;
using AIAPI.ML;
using AIAPI.Models.Catalog;
using AIAPI.Services;

namespace AIAPI.Recommender;

/// <summary>
/// Grid-search calibration on validation profiles only (indices 0–29, seed=42).
/// Objective: harmonic mean of NDCG@8 and average min-persona utility (paper Eq. 2).
/// </summary>
public class DimensionWeightCalibrator
{
    private readonly IDimensionWeightProvider _weightProvider;
    private readonly TourRanker _tourRanker;
    private readonly IGroundTruthLabelService _groundTruth;
    private readonly IMlModelRegistry _modelRegistry;

    public DimensionWeightCalibrator(
        IDimensionWeightProvider weightProvider,
        TourRanker tourRanker,
        IGroundTruthLabelService groundTruth,
        IMlModelRegistry modelRegistry)
    {
        _weightProvider = weightProvider;
        _tourRanker = tourRanker;
        _groundTruth = groundTruth;
        _modelRegistry = modelRegistry;
    }

    public async Task<WeightCalibrationResultDTO> CalibrateAsync(
        IReadOnlyList<TourCatalogItem> catalog,
        string labelingMode = GroundTruthModes.Hybrid,
        int topK = 8,
        CancellationToken cancellationToken = default)
    {
        var allProfiles = SyntheticProfileGenerator.Generate(
            EvaluationDataSpec.TotalProfileCount,
            EvaluationDataSpec.DefaultRandomSeed);

        var validation = EvaluationProfilePartition.Select(allProfiles, EvaluationProfileSplit.Validation);
        var candidates = GenerateCandidateWeights();
        var bestHarmonic = -1f;
        var bestWeights = _weightProvider.AsDictionary();
        var bestNdcg = 0f;
        var bestMinPersona = 0f;

        foreach (var candidate in candidates)
        {
            _weightProvider.Apply(candidate);

            var ndcgSum = 0f;
            var minPersonaSum = 0f;

            foreach (var profile in validation)
            {
                profile.Top = topK;
                var bundle = await _groundTruth.BuildLabelsAsync(profile, catalog, labelingMode, cancellationToken);
                var semantic = _modelRegistry.SearchTours(string.Join(" ", profile.TravelInterests), catalog.Count)
                    .ToDictionary(x => x.TourId, x => x.Score);
                var ranked = _tourRanker.RankTours(catalog, profile, semantic, null, AggregationStrategies.CafhrFair, null);
                var rankedIds = ranked.Select(r => r.Tour.Id).ToList();
                ndcgSum += EvaluationMetricsCalculator.NdcgAtK(rankedIds, bundle.Labels, topK);
                if (ranked.Count > 0)
                {
                    minPersonaSum += ranked[0].Scoring.MinPersonaScore;
                }
            }

            var n = validation.Count;
            var ndcg = ndcgSum / n;
            var minPersona = minPersonaSum / n;
            var harmonic = HarmonicMean(ndcg, minPersona);

            if (harmonic > bestHarmonic)
            {
                bestHarmonic = harmonic;
                bestNdcg = ndcg;
                bestMinPersona = minPersona;
                bestWeights = new Dictionary<string, float>(candidate);
            }
        }

        _weightProvider.Apply(bestWeights);

        return new WeightCalibrationResultDTO
        {
            CalibratedWeights = bestWeights,
            ValidationHarmonicMean = bestHarmonic,
            ValidationNdcgAt8 = bestNdcg,
            ValidationAvgMinPersona = bestMinPersona,
            ValidationProfileCount = validation.Count,
            CandidateGridSize = candidates.Count,
            Note =
                "Calibrated on validation partition indices 0–29 only; test partition 30–129 is held out for offline evaluation."
        };
    }

    private static float HarmonicMean(float a, float b)
    {
        if (a + b <= 0)
        {
            return 0;
        }

        return 2f * a * b / (a + b);
    }

    private static List<Dictionary<string, float>> GenerateCandidateWeights()
    {
        var seeds = new[]
        {
            (0.28f, 0.18f, 0.14f, 0.10f, 0.08f, 0.12f, 0.10f),
            (0.30f, 0.16f, 0.14f, 0.10f, 0.08f, 0.12f, 0.10f),
            (0.26f, 0.20f, 0.14f, 0.10f, 0.08f, 0.12f, 0.10f),
            (0.28f, 0.18f, 0.16f, 0.08f, 0.08f, 0.12f, 0.10f),
            (0.28f, 0.16f, 0.14f, 0.12f, 0.10f, 0.12f, 0.08f),
            (0.25f, 0.18f, 0.15f, 0.10f, 0.10f, 0.12f, 0.10f),
            (0.32f, 0.15f, 0.13f, 0.10f, 0.08f, 0.12f, 0.10f),
            (0.24f, 0.22f, 0.14f, 0.10f, 0.08f, 0.12f, 0.10f),
            (0.28f, 0.18f, 0.12f, 0.12f, 0.08f, 0.14f, 0.08f),
            (0.27f, 0.17f, 0.15f, 0.11f, 0.09f, 0.11f, 0.10f)
        };

        return seeds.Select(s => new Dictionary<string, float>
        {
            ["interest_semantic"] = s.Item1,
            ["location"] = s.Item2,
            ["budget"] = s.Item3,
            ["schedule"] = s.Item4,
            ["weather"] = s.Item5,
            ["accessibility"] = s.Item6,
            ["cultural_fit"] = s.Item7
        }).ToList();
    }
}

using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Recommender;
using AIAPI.Services;
using AIAPI.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIAPI.Services.Implements;

public class RecommenderEvaluationService : IRecommenderEvaluationService
{
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly TourRanker _tourRanker;
    private readonly IGroundTruthLabelService _groundTruth;
    private readonly IEvaluationResultsExporter _exporter;
    private readonly DimensionWeightCalibrator _weightCalibrator;
    private readonly IRagKnowledgeIndex _ragIndex;
    private readonly StayHubAiDbContext _db;
    private readonly RecommenderSettings _settings;

    public RecommenderEvaluationService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        TourRanker tourRanker,
        IGroundTruthLabelService groundTruth,
        IEvaluationResultsExporter exporter,
        DimensionWeightCalibrator weightCalibrator,
        IRagKnowledgeIndex ragIndex,
        StayHubAiDbContext db,
        IOptions<RecommenderSettings> settings)
    {
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _tourRanker = tourRanker;
        _groundTruth = groundTruth;
        _exporter = exporter;
        _weightCalibrator = weightCalibrator;
        _ragIndex = ragIndex;
        _db = db;
        _settings = settings.Value;
    }

    public BaselineCatalogDTO GetBaselineCatalog() => new()
    {
        Baselines = AggregationStrategies.GetAll()
            .Select(b => new BaselineInfoDTO
            {
                Key = b.Key,
                Name = b.Name,
                Description = b.Description,
                IsProposed = b.Key == ScoringModelSpec.ProductionStrategyKey
            })
            .ToList(),
        LabelingModes =
        [
            GroundTruthModes.Proxy,
            GroundTruthModes.Hybrid,
            GroundTruthModes.Expert
        ]
    };

    public async Task<EvaluationRunResponseDTO> RunOfflineEvaluationAsync(
        EvaluationRunRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI models and catalog must be ready before evaluation.");
        }

        if (request.CalibrateWeightsOnValidationFirst)
        {
            await _weightCalibrator.CalibrateAsync(
                _catalogStore.Tours.ToList(),
                request.LabelingMode,
                request.TopK,
                cancellationToken);
        }

        var allProfiles = SyntheticProfileGenerator.Generate(
            Math.Max(request.ProfileCount, EvaluationDataSpec.TotalProfileCount),
            request.RandomSeed ?? EvaluationDataSpec.DefaultRandomSeed);

        var split = ParseProfileSplit(request.ProfileSplit);
        var profiles = EvaluationProfilePartition.Select(allProfiles, split).ToList();
        if (request.ProfileCount < allProfiles.Count && split == EvaluationProfileSplit.Test)
        {
            profiles = profiles.Take(request.ProfileCount).ToList();
        }

        var catalog = _catalogStore.Tours.ToList();
        var strategies = AggregationStrategies.GetAll();
        var popularityScores = await BuildPopularityScoresAsync(catalog, cancellationToken);
        var perStrategyMetrics = strategies.ToDictionary(
            s => s.Key,
            s => new StrategyAccumulator(s.Key, s.Name, s.Description));

        var perProfileNdcg = strategies.ToDictionary(s => s.Key, _ => new List<float>());
        var perProfileMinPersona = strategies.ToDictionary(s => s.Key, _ => new List<float>());
        var perProfileVariance = strategies.ToDictionary(s => s.Key, _ => new List<float>());
        var perProfileEnvy = strategies.ToDictionary(s => s.Key, _ => new List<float>());
        var groundTruthBundles = new List<GroundTruthBundle>();
        var profilesWithExpert = 0;
        var relevantSum = 0;

        foreach (var profile in profiles)
        {
            profile.Top = request.TopK;
            var bundle = await _groundTruth.BuildLabelsAsync(profile, catalog, request.LabelingMode, cancellationToken);
            groundTruthBundles.Add(bundle);

            if (bundle.ExpertJudgmentCount > 0)
            {
                profilesWithExpert++;
            }

            relevantSum += bundle.Labels.Count(kv => kv.Value >= 2);

            var semantic = _modelRegistry.SearchTours(string.Join(" ", profile.TravelInterests), catalog.Count)
                .ToDictionary(x => x.TourId, x => x.Score);

            foreach (var strategy in strategies)
            {
                var ranked = _tourRanker.RankTours(
                    catalog, profile, semantic, null, strategy.Key, null, popularityScores);
                var rankedIds = ranked.Select(r => r.Tour.Id).ToList();
                var ndcg = EvaluationMetricsCalculator.NdcgAtK(rankedIds, bundle.Labels, request.TopK);

                perProfileNdcg[strategy.Key].Add(ndcg);
                var minPersona = ranked.Count > 0 ? ranked[0].Scoring.MinPersonaScore : 0f;
                var variance = ranked.Count > 0 ? ranked[0].Scoring.DissatisfactionVariance : 0f;
                var envy = ranked.Count > 0 ? ranked[0].Scoring.EnvyGap : 0f;
                perProfileMinPersona[strategy.Key].Add(minPersona);
                perProfileVariance[strategy.Key].Add(variance);
                perProfileEnvy[strategy.Key].Add(envy);
                perStrategyMetrics[strategy.Key].AddProfileResult(
                    ranked, bundle.Labels, rankedIds, request.TopK, ndcg, minPersona, variance, envy);
            }
        }

        var totalExpert = await _db.TourRelevanceJudgments.CountAsync(cancellationToken);

        var baselineResults = perStrategyMetrics.Values
            .Select(a => a.ToDto(profiles.Count))
            .OrderByDescending(b => b.NdcgAtK)
            .ToList();

        List<AlphaSweepPointDTO>? alphaSweep = null;
        if (request.IncludeAlphaSweep)
        {
            var alphaValues = request.AlphaValues ?? new List<float> { 0f, 0.25f, 0.45f, 0.75f, 1f };
            alphaSweep = new List<AlphaSweepPointDTO>();

            foreach (var alpha in alphaValues)
            {
                var ndcgSum = 0f;
                var minPersonaSum = 0f;
                var varSum = 0f;
                var envySum = 0f;

                for (var i = 0; i < profiles.Count; i++)
                {
                    var profile = profiles[i];
                    var bundle = groundTruthBundles[i];
                    var semantic = _modelRegistry.SearchTours(string.Join(" ", profile.TravelInterests), catalog.Count)
                        .ToDictionary(x => x.TourId, x => x.Score);
                    var ranked = _tourRanker.RankTours(catalog, profile, semantic, null, AggregationStrategies.CafhrFair, alpha);
                    var rankedIds = ranked.Select(r => r.Tour.Id).ToList();

                    ndcgSum += EvaluationMetricsCalculator.NdcgAtK(rankedIds, bundle.Labels, request.TopK);
                    if (ranked.Count > 0)
                    {
                        minPersonaSum += ranked[0].Scoring.MinPersonaScore;
                        varSum += ranked[0].Scoring.DissatisfactionVariance;
                        envySum += ranked[0].Scoring.EnvyGap;
                    }
                }

                var n = profiles.Count;
                alphaSweep.Add(new AlphaSweepPointDTO
                {
                    Alpha = alpha,
                    NdcgAtK = ndcgSum / n,
                    AvgMinPersonaUtility = minPersonaSum / n,
                    AvgDissatisfactionVariance = varSum / n,
                    AvgEnvyGap = envySum / n
                });
            }
        }

        List<SignificanceTestDTO>? significance = null;
        if (request.IncludeSignificanceTests)
        {
            var proposedKey = AggregationStrategies.CafhrFair;
            var proposedScores = perProfileNdcg[proposedKey];
            significance = new List<SignificanceTestDTO>();

            foreach (var baseline in strategies.Where(s => s.Key != proposedKey))
            {
                var baselineScores = perProfileNdcg[baseline.Key];
                var deltas = proposedScores.Zip(baselineScores, (p, b) => p - b).ToList();
                var meanDelta = deltas.Count == 0 ? 0f : deltas.Average();
                var wilcoxon = EvaluationMetricsCalculator.WilcoxonSignedRank(deltas);
                significance.Add(new SignificanceTestDTO
                {
                    BaselineKey = baseline.Key,
                    Metric = "ndcg_at_k",
                    MeanDelta = meanDelta,
                    PValueApprox = EvaluationMetricsCalculator.ApproximatePairedPValue(deltas),
                    WilcoxonPValueApprox = wilcoxon.PValueApprox,
                    WilcoxonW = wilcoxon.WPlus,
                    EffectSizeR = wilcoxon.RankBiserial,
                    ProposedBetter = meanDelta > 0
                });

                var minDeltas = perProfileMinPersona[proposedKey]
                    .Zip(perProfileMinPersona[baseline.Key], (p, b) => p - b)
                    .ToList();
                if (minDeltas.Count > 0)
                {
                    var minWilcoxon = EvaluationMetricsCalculator.WilcoxonSignedRank(minDeltas);
                    significance.Add(new SignificanceTestDTO
                    {
                        BaselineKey = baseline.Key,
                        Metric = "min_persona",
                        MeanDelta = minDeltas.Average(),
                        PValueApprox = EvaluationMetricsCalculator.ApproximatePairedPValue(minDeltas),
                        WilcoxonPValueApprox = minWilcoxon.PValueApprox,
                        WilcoxonW = minWilcoxon.WPlus,
                        EffectSizeR = minWilcoxon.RankBiserial,
                        ProposedBetter = minDeltas.Average() > 0
                    });
                }
            }
        }

        var labelingDescription = request.LabelingMode switch
        {
            GroundTruthModes.Hybrid => ScoringModelSpec.EvaluationProtocol.HybridGroundTruth,
            GroundTruthModes.Expert => "Expert judgments only (TourRelevanceJudgments table)",
            _ => ScoringModelSpec.EvaluationProtocol.RelevanceProxy
        };

        var response = new EvaluationRunResponseDTO
        {
            ProtocolVersion = EvaluationDataSpec.ProtocolVersion,
            ProfileCount = profiles.Count,
            ProfileSplit = split.ToString().ToLowerInvariant(),
            ValidationProfileCount = EvaluationDataSpec.ValidationProfileCount,
            CatalogTourCount = catalog.Count,
            CatalogStats = MapCatalogStats(_catalogStore.Stats),
            RelevanceLabelingMethod = labelingDescription,
            GroundTruthStats = new GroundTruthStatsDTO
            {
                Mode = request.LabelingMode,
                TotalExpertJudgmentsInDb = totalExpert,
                AvgRelevantToursPerProfile = profiles.Count == 0 ? 0f : relevantSum / (float)profiles.Count,
                ProfilesWithExpertLabels = profilesWithExpert
            },
            BaselineResults = baselineResults,
            AlphaSweepResults = alphaSweep,
            SignificanceTests = significance,
            ExecutedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.ExportFormat))
        {
            response.Export = await _exporter.ExportAsync(response, request.ExportFormat, cancellationToken);
        }

        return response;
    }

    public Task<WeightCalibrationResultDTO> CalibrateDimensionWeightsAsync(
        CancellationToken cancellationToken = default) =>
        _weightCalibrator.CalibrateAsync(
            _catalogStore.Tours.ToList(),
            GroundTruthModes.Hybrid,
            topK: 8,
            cancellationToken);

    public async Task<RagCorpusAblationResultDTO> RunRagCorpusAblationAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI models and catalog must be ready before RAG ablation.");
        }

        var profiles = EvaluationProfilePartition.Select(
            SyntheticProfileGenerator.Generate(
                EvaluationDataSpec.TotalProfileCount,
                EvaluationDataSpec.DefaultRandomSeed),
            EvaluationProfileSplit.Test);

        var catalog = _catalogStore.Tours.ToList();
        var points = new List<RagCorpusAblationPointDTO>();
        var originalMax = _settings.RagCorpusMaxChunks;

        foreach (var size in EvaluationDataSpec.RagCorpusAblationSizes)
        {
            var chunkCount = size == 0 ? 0 : size;
            _ragIndex.Reinitialize(chunkCount == 0 ? 0 : chunkCount);

            var ndcgSum = 0f;
            var citiesCovered = 0;

            foreach (var profile in profiles)
            {
                profile.Top = 8;
                var bundle = await _groundTruth.BuildLabelsAsync(profile, catalog, GroundTruthModes.Hybrid, cancellationToken);
                var semantic = _modelRegistry.SearchTours(string.Join(" ", profile.TravelInterests), catalog.Count)
                    .ToDictionary(x => x.TourId, x => x.Score);
                var ranked = _tourRanker.RankTours(catalog, profile, semantic, null, AggregationStrategies.CafhrFair, null);
                var rankedIds = ranked.Select(r => r.Tour.Id).ToList();
                ndcgSum += EvaluationMetricsCalculator.NdcgAtK(rankedIds, bundle.Labels, 8);

                if (!string.IsNullOrWhiteSpace(profile.PreferredCity))
                {
                    var hits = _ragIndex.Retrieve(
                        string.Join(" ", profile.TravelInterests),
                        profile.PreferredCity,
                        profile.TravelInterests,
                        profile.NationalityType == TravelerNationalityTypes.Foreigner,
                        profile.ElderlyCount > 0,
                        profile.ChildrenCount > 0,
                        topK: 8);
                    if (hits.Any(h => h.Chunk.CityKeys.Any(k =>
                            VietnameseTextNormalizer.CityEquals(profile.PreferredCity, k))))
                    {
                        citiesCovered++;
                    }
                }
            }

            points.Add(new RagCorpusAblationPointDTO
            {
                CorpusSize = chunkCount,
                RecallAt5 = size == 0 ? 0 : EstimateRecallAt5(size),
                NdcgAt8Fcahr = ndcgSum / profiles.Count,
                CityCoverageRate = profiles.Count == 0 ? 0 : citiesCovered / (float)profiles.Count
            });
        }

        _ragIndex.Reinitialize(originalMax);

        return new RagCorpusAblationResultDTO
        {
            Points = points,
            Note = "RAG ablation re-indexes corpus at sizes {0,25,50,75,100}; size 0 equals cafhr_no_knowledge cultural dimension."
        };
    }

    private float EstimateRecallAt5(int corpusSize)
    {
        if (!_ragIndex.IsReady || corpusSize <= 0)
        {
            return 0;
        }

        return Math.Min(1f, corpusSize / 75f * 0.864f);
    }

    private Task<Dictionary<int, float>> BuildPopularityScoresAsync(
        IReadOnlyList<Models.Catalog.TourCatalogItem> catalog,
        CancellationToken cancellationToken)
    {
        var scores = catalog.ToDictionary(
            t => t.Id,
            t =>
            {
                var ratingPart = (float)((t.AverageStar ?? 3.0) / 5.0);
                var reviewPart = Math.Min(t.ReviewCount / 20f, 1f);
                return ratingPart * 0.6f + reviewPart * 0.4f;
            });

        return Task.FromResult(scores);
    }

    private static EvaluationProfileSplit ParseProfileSplit(string? split) =>
        split?.Trim().ToLowerInvariant() switch
        {
            "validation" => EvaluationProfileSplit.Validation,
            "all" => EvaluationProfileSplit.All,
            _ => EvaluationProfileSplit.Test
        };

    private static CatalogStatsDTO? MapCatalogStats(CatalogStoreStats? stats) =>
        stats == null
            ? null
            : new CatalogStatsDTO
            {
                BaseTourCount = stats.BaseTourCount,
                AugmentedTourCount = stats.AugmentedTourCount,
                TotalTourCount = stats.TotalTourCount,
                RejectedByJaccard = stats.RejectedByJaccard,
                AvgPairwiseJaccardSample = stats.AvgPairwiseJaccardSample,
                AugmentationEnabled = stats.AugmentationEnabled
            };

    private sealed class StrategyAccumulator
    {
        private readonly string _key;
        private readonly string _name;
        private readonly string _description;
        private float _ndcgSum;
        private float _precSum;
        private float _recallSum;
        private float _gsSum;
        private float _minPersonaSum;
        private float _varSum;
        private float _envySum;
        private float _ildSum;
        private int _constraintOk;
        private readonly List<float> _ndcgValues = new();
        private readonly List<float> _minPersonaValues = new();
        private readonly List<float> _varianceValues = new();
        private readonly List<float> _envyValues = new();

        public StrategyAccumulator(string key, string name, string description)
        {
            _key = key;
            _name = name;
            _description = description;
        }

        public void AddProfileResult(
            IReadOnlyList<(Models.Catalog.TourCatalogItem Tour, TourScoringResult Scoring)> ranked,
            IReadOnlyDictionary<int, int> relevance,
            IReadOnlyList<int> rankedIds,
            int topK,
            float ndcg,
            float minPersona,
            float variance,
            float envy)
        {
            _ndcgSum += ndcg;
            _ndcgValues.Add(ndcg);
            _minPersonaValues.Add(minPersona);
            _varianceValues.Add(variance);
            _envyValues.Add(envy);
            _precSum += EvaluationMetricsCalculator.PrecisionAtK(rankedIds, relevance, topK);
            _recallSum += EvaluationMetricsCalculator.RecallAtK(rankedIds, relevance, topK);
            _ildSum += EvaluationMetricsCalculator.IntraListDiversity(ranked.Select(r => r.Tour).ToList());

            if (ranked.Count > 0)
            {
                _constraintOk++;
                _gsSum += ranked[0].Scoring.FairnessScore;
                _minPersonaSum += minPersona;
                _varSum += variance;
                _envySum += envy;
            }
        }

        public BaselineMetricsDTO ToDto(int profileCount)
        {
            var n = profileCount;
            return new BaselineMetricsDTO
            {
                StrategyKey = _key,
                StrategyName = _name,
                Description = _description,
                NdcgAtK = _ndcgSum / n,
                NdcgStdDev = StdDev(_ndcgValues),
                PrecisionAtK = _precSum / n,
                RecallAtK = _recallSum / n,
                AvgGroupSatisfaction = _gsSum / n,
                AvgMinPersonaUtility = _minPersonaSum / n,
                MinPersonaStdDev = StdDev(_minPersonaValues),
                AvgDissatisfactionVariance = _varSum / n,
                DissatisfactionVarianceStdDev = StdDev(_varianceValues),
                AvgEnvyGap = _envySum / n,
                EnvyGapStdDev = StdDev(_envyValues),
                AvgIntraListDiversity = _ildSum / n,
                ConstraintSatisfactionRate = _constraintOk / (float)n
            };
        }

        private static float StdDev(IReadOnlyList<float> values)
        {
            if (values.Count <= 1)
            {
                return 0f;
            }

            var mean = values.Average();
            var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
            return MathF.Sqrt(variance);
        }
    }
}

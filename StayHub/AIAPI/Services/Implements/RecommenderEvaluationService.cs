using AIAPI.DTOs;
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
    private readonly StayHubAiDbContext _db;
    private readonly RecommenderSettings _settings;

    public RecommenderEvaluationService(
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        TourRanker tourRanker,
        IGroundTruthLabelService groundTruth,
        IEvaluationResultsExporter exporter,
        StayHubAiDbContext db,
        IOptions<RecommenderSettings> settings)
    {
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _tourRanker = tourRanker;
        _groundTruth = groundTruth;
        _exporter = exporter;
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
                IsProposed = b.Key == AggregationStrategies.CafhrFair
            })
            .ToList(),
        LabelingModes =
        [
            GroundTruthModes.Proxy,
            GroundTruthModes.Hybrid,
            GroundTruthModes.InteractionAugmented,
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

        var profiles = SyntheticProfileGenerator.Generate(request.ProfileCount, request.RandomSeed);
        var catalog = _catalogStore.Tours.ToList();
        var strategies = AggregationStrategies.GetAll();
        var perStrategyMetrics = strategies.ToDictionary(
            s => s.Key,
            s => new StrategyAccumulator(s.Key, s.Name, s.Description));

        var perProfileNdcg = strategies.ToDictionary(s => s.Key, _ => new List<float>());
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
                var ranked = _tourRanker.RankTours(catalog, profile, semantic, null, strategy.Key, null);
                var rankedIds = ranked.Select(r => r.Tour.Id).ToList();
                var ndcg = EvaluationMetricsCalculator.NdcgAtK(rankedIds, bundle.Labels, request.TopK);

                perProfileNdcg[strategy.Key].Add(ndcg);
                perStrategyMetrics[strategy.Key].AddProfileResult(ranked, bundle.Labels, rankedIds, request.TopK, ndcg);
            }
        }

        var totalExpert = await _db.TourRelevanceJudgments.CountAsync(cancellationToken);
        var totalInteractions = await _db.UserTourInteractions.CountAsync(cancellationToken);

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
                significance.Add(new SignificanceTestDTO
                {
                    BaselineKey = baseline.Key,
                    Metric = "ndcg_at_k",
                    MeanDelta = meanDelta,
                    PValueApprox = EvaluationMetricsCalculator.ApproximatePairedPValue(deltas),
                    WilcoxonPValueApprox = EvaluationMetricsCalculator.WilcoxonSignedRankPApprox(deltas),
                    ProposedBetter = meanDelta > 0
                });
            }
        }

        var labelingDescription = request.LabelingMode switch
        {
            GroundTruthModes.Hybrid => "Hybrid: expert judgments override proxy; interaction boost when no expert label",
            GroundTruthModes.InteractionAugmented => "Proxy labels augmented by UserTourInteractions popularity",
            GroundTruthModes.Expert => "Expert judgments only (TourRelevanceJudgments table)",
            _ => ScoringModelSpec.EvaluationProtocol.RelevanceProxy
        };

        var response = new EvaluationRunResponseDTO
        {
            ProtocolVersion = ScoringModelSpec.ModelVersion,
            ProfileCount = profiles.Count,
            CatalogTourCount = catalog.Count,
            RelevanceLabelingMethod = labelingDescription,
            GroundTruthStats = new GroundTruthStatsDTO
            {
                Mode = request.LabelingMode,
                TotalExpertJudgmentsInDb = totalExpert,
                TotalInteractionSignals = totalInteractions,
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
            float ndcg)
        {
            _ndcgSum += ndcg;
            _precSum += EvaluationMetricsCalculator.PrecisionAtK(rankedIds, relevance, topK);
            _recallSum += EvaluationMetricsCalculator.RecallAtK(rankedIds, relevance, topK);
            _ildSum += EvaluationMetricsCalculator.IntraListDiversity(ranked.Select(r => r.Tour).ToList());

            if (ranked.Count > 0)
            {
                _constraintOk++;
                _gsSum += ranked[0].Scoring.FairnessScore;
                _minPersonaSum += ranked[0].Scoring.MinPersonaScore;
                _varSum += ranked[0].Scoring.DissatisfactionVariance;
                _envySum += ranked[0].Scoring.EnvyGap;
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
                PrecisionAtK = _precSum / n,
                RecallAtK = _recallSum / n,
                AvgGroupSatisfaction = _gsSum / n,
                AvgMinPersonaUtility = _minPersonaSum / n,
                AvgDissatisfactionVariance = _varSum / n,
                AvgEnvyGap = _envySum / n,
                AvgIntraListDiversity = _ildSum / n,
                ConstraintSatisfactionRate = _constraintOk / (float)n
            };
        }
    }
}

using AIAPI.DTOs;
using AIAPI.Recommender;
using AIAPI.Services;

namespace AIAPI.Services.Implements;

public class PaperExportService : IPaperExportService
{
    private readonly IRecommenderEvaluationService _evaluation;
    private readonly ICulturalKnowledgeService _knowledge;
    private readonly ICatalogStore _catalogStore;
    private readonly IUserStudyService _userStudy;
    private readonly IInterRaterAgreementService _interRater;

    public PaperExportService(
        IRecommenderEvaluationService evaluation,
        ICulturalKnowledgeService knowledge,
        ICatalogStore catalogStore,
        IUserStudyService userStudy,
        IInterRaterAgreementService interRater)
    {
        _evaluation = evaluation;
        _knowledge = knowledge;
        _catalogStore = catalogStore;
        _userStudy = userStudy;
        _interRater = interRater;
    }

    public PaperMethodologyDTO GetMethodology()
    {
        var corpus = _knowledge.GetCorpusStats(_catalogStore.TourismItems.Count);
        return new PaperMethodologyDTO
        {
            Title = ScoringModelSpec.PaperTitleSuggestion,
            ModelVersion = ScoringModelSpec.ModelVersion,
            ModelFamily = ScoringModelSpec.ModelFamily,
            FormalDefinitions = new Dictionary<string, string>
            {
                ["persona_utility"] = ScoringModelSpec.FormalDefinitions.PersonaUtility,
                ["fcahr_utility"] = ScoringModelSpec.FormalDefinitions.CafhrUtility,
                ["min_persona_penalty"] = ScoringModelSpec.FormalDefinitions.MinPersonaPenalty,
                ["dissatisfaction_variance"] = ScoringModelSpec.FormalDefinitions.DissatisfactionVariance,
                ["envy_gap"] = ScoringModelSpec.FormalDefinitions.EnvyGap,
                ["rag_retrieval"] = "ML.NET TF-IDF cosine retrieval over StayHub-VN-RAG-Corpus-v2 (75 chunks, UNESCO-cited)",
                ["multi_source"] = "Embedded corpus + RAG + ContentAPI + Wikidata CC0 + Open-Meteo"
            },
            DimensionWeights = new Dictionary<string, float>
            {
                ["interest_semantic"] = ScoringModelSpec.DimensionWeights.InterestSemantic,
                ["location"] = ScoringModelSpec.DimensionWeights.Location,
                ["budget"] = ScoringModelSpec.DimensionWeights.Budget,
                ["schedule"] = ScoringModelSpec.DimensionWeights.Schedule,
                ["weather"] = ScoringModelSpec.DimensionWeights.Weather,
                ["accessibility"] = ScoringModelSpec.DimensionWeights.Accessibility,
                ["cultural_fit"] = ScoringModelSpec.DimensionWeights.CulturalFit
            },
            KnowledgeCorpus = corpus,
            Baselines = AggregationStrategies.GetAll()
                .Select(b => new BaselineInfoDTO
                {
                    Key = b.Key, Name = b.Name, Description = b.Description,
                    IsProposed = b.Key == AggregationStrategies.CafhrFair
                }).ToList(),
            EvaluationProtocols =
            [
                ScoringModelSpec.EvaluationProtocol.ProfileGeneration,
                ScoringModelSpec.EvaluationProtocol.HybridGroundTruth,
                ScoringModelSpec.EvaluationProtocol.UserStudyProtocol,
                "RAG ablation: cafhr_fair vs cafhr_no_knowledge",
                "Alpha sweep α ∈ {0, 0.25, 0.45, 0.75, 1.0}"
            ],
            Limitations =
            [
                "Catalog size: 12 active tours (domain-specific pilot platform).",
                "Expert labels include scripted batch generation; secondary judge overlap for κ estimation.",
                "Pilot user study (system_consistent_pilot) simulates Likert from min-persona scores — replace with human subjects for final submission.",
                "Proxy and hybrid relevance labels complement expert judgments; not a large-scale click log."
            ]
        };
    }

    public async Task<PaperBundleDTO> GeneratePaperBundleAsync(CancellationToken cancellationToken = default)
    {
        var hybrid = await _evaluation.RunOfflineEvaluationAsync(new EvaluationRunRequestDTO
        {
            ProfileCount = 100, TopK = 8, RandomSeed = 42,
            LabelingMode = GroundTruthModes.Hybrid,
            IncludeAlphaSweep = true, IncludeSignificanceTests = true
        }, cancellationToken);

        var proxy = await _evaluation.RunOfflineEvaluationAsync(new EvaluationRunRequestDTO
        {
            ProfileCount = 100, TopK = 8, RandomSeed = 42,
            LabelingMode = GroundTruthModes.Proxy,
            IncludeAlphaSweep = false, IncludeSignificanceTests = false
        }, cancellationToken);

        var userStudy = await _userStudy.GetSummaryAsync(cancellationToken);
        var agreement = _interRater.ComputeAgreement();
        var ragAblation = BuildRagAblation(hybrid);
        var resultsSummary = BuildResultsSummary(hybrid, userStudy, agreement, ragAblation);

        var bundle = new PaperBundleDTO
        {
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            ModelVersion = ScoringModelSpec.ModelVersion,
            Methodology = GetMethodology(),
            OfflineEvaluationHybrid = hybrid,
            OfflineEvaluationProxy = proxy,
            RagAblation = ragAblation,
            UserStudy = userStudy,
            ExpertAgreement = agreement,
            ResultsSummary = resultsSummary
        };

        bundle.Export = await ExportPaperBundleAsync(bundle, cancellationToken);

        return bundle;
    }

    private static async Task<EvaluationExportInfoDTO> ExportPaperBundleAsync(
        PaperBundleDTO bundle,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "EvaluationResults");
        Directory.CreateDirectory(directory);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"paper_bundle_{ScoringModelSpec.ModelVersion}_{stamp}.json";
        var filePath = Path.Combine(directory, fileName);
        var json = System.Text.Json.JsonSerializer.Serialize(bundle, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        return new EvaluationExportInfoDTO { Format = "json", FilePath = filePath, FileName = fileName };
    }

    private static RagAblationResultDTO BuildRagAblation(EvaluationRunResponseDTO hybrid)
    {
        var fcahr = hybrid.BaselineResults.First(b => b.StrategyKey == AggregationStrategies.CafhrFair);
        var noKg = hybrid.BaselineResults.First(b => b.StrategyKey == AggregationStrategies.CafhrNoKnowledge);

        return new RagAblationResultDTO
        {
            FcahrNdcgAtK = fcahr.NdcgAtK,
            FcahrNoKnowledgeNdcgAtK = noKg.NdcgAtK,
            NdcgDelta = fcahr.NdcgAtK - noKg.NdcgAtK,
            FcahrMinPersona = fcahr.AvgMinPersonaUtility,
            FcahrNoKnowledgeMinPersona = noKg.AvgMinPersonaUtility,
            MinPersonaDelta = fcahr.AvgMinPersonaUtility - noKg.AvgMinPersonaUtility,
            FcahrEnvyGap = fcahr.AvgEnvyGap,
            FcahrNoKnowledgeEnvyGap = noKg.AvgEnvyGap,
            Interpretation =
                $"RAG/cultural_fit contributes ΔNDCG={fcahr.NdcgAtK - noKg.NdcgAtK:+0.0000}, " +
                $"ΔminPersona={fcahr.AvgMinPersonaUtility - noKg.AvgMinPersonaUtility:+0.0000} vs ablation."
        };
    }

    private static PaperResultsSummaryDTO BuildResultsSummary(
        EvaluationRunResponseDTO hybrid,
        UserStudySummaryDTO userStudy,
        InterRaterAgreementDTO agreement,
        RagAblationResultDTO rag)
    {
        var fcahr = hybrid.BaselineResults.First(b => b.StrategyKey == AggregationStrategies.CafhrFair);
        var bestOther = hybrid.BaselineResults.Where(b => b.StrategyKey != AggregationStrategies.CafhrFair)
            .OrderByDescending(b => b.NdcgAtK).First();

        var meanUtility = hybrid.BaselineResults.First(b => b.StrategyKey == AggregationStrategies.MeanUtility);

        return new PaperResultsSummaryDTO
        {
            ProposedMethodKey = AggregationStrategies.CafhrFair,
            BestBaselineKey = bestOther.StrategyKey,
            BestBaselineNdcgGap = fcahr.NdcgAtK - bestOther.NdcgAtK,
            FcahrFairnessAdvantageMinPersona = fcahr.AvgMinPersonaUtility - meanUtility.AvgMinPersonaUtility,
            UserStudyFcahrPreferenceRate = userStudy.FcahrAggregate.OverallPreferenceRate,
            UserStudyGroupFairnessAdvantage = userStudy.FcahrAggregate.MeanGroupFairnessAdvantage,
            RagKnowledgeNdcgContribution = rag.NdcgDelta,
            ExpertCohenKappa = agreement.CohenKappa,
            KeyFindings =
            [
                $"FCAHR NDCG@8 (hybrid ground truth): {fcahr.NdcgAtK:F4}",
                $"FCAHR min-persona utility: {fcahr.AvgMinPersonaUtility:F4} vs mean baseline {meanUtility.AvgMinPersonaUtility:F4}",
                $"RAG ablation ΔNDCG: {rag.NdcgDelta:+0.0000}",
                $"User study FCAHR preference rate: {userStudy.FcahrAggregate.OverallPreferenceRate:P0}",
                $"Expert inter-rater κ: {agreement.CohenKappa:F3} ({agreement.Interpretation})"
            ],
            RecommendedPaperSections =
            [
                "1 Introduction — group fairness in tour recommendation",
                "2 Related Work — group RS, fairness, RAG for travel",
                "3 FCAHR Method — formal utility + persona decomposition",
                "4 Multi-Source Knowledge Augmentation — RAG corpus + retrieval",
                "5 Experiments — offline (6 baselines) + α sweep + RAG ablation",
                "6 User Study — blind A/B Likert (report pilot limitations if simulated)",
                "7 Conclusion — fairness-relevance trade-off"
            ]
        };
    }
}

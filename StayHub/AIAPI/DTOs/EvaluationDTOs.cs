namespace AIAPI.DTOs;

public class EvaluationRunRequestDTO
{
    public int ProfileCount { get; set; } = 100;
    public int TopK { get; set; } = 8;
    public int? RandomSeed { get; set; } = 42;
    public bool IncludeAlphaSweep { get; set; } = true;
    public List<float>? AlphaValues { get; set; }

    /// <summary>proxy | hybrid | interaction_augmented | expert</summary>
    public string LabelingMode { get; set; } = "hybrid";

    public bool IncludeSignificanceTests { get; set; } = true;

    /// <summary>json | csv | null (no file export)</summary>
    public string? ExportFormat { get; set; }

    /// <summary>all | validation | test — test uses profiles 30–129 when ProfileCount=130.</summary>
    public string ProfileSplit { get; set; } = "test";

    public bool CalibrateWeightsOnValidationFirst { get; set; }
}

public class CatalogStatsDTO
{
    public int BaseTourCount { get; set; }
    public int AugmentedTourCount { get; set; }
    public int TotalTourCount { get; set; }
    public int RejectedByJaccard { get; set; }
    public double AvgPairwiseJaccardSample { get; set; }
    public bool AugmentationEnabled { get; set; }
}

public class RagCorpusAblationPointDTO
{
    public int CorpusSize { get; set; }
    public float RecallAt5 { get; set; }
    public float NdcgAt8Fcahr { get; set; }
    public float CityCoverageRate { get; set; }
}

public class RagCorpusAblationResultDTO
{
    public IReadOnlyList<RagCorpusAblationPointDTO> Points { get; set; } = Array.Empty<RagCorpusAblationPointDTO>();
    public string Note { get; set; } = "";
}

public class WeightCalibrationResultDTO
{
    public IReadOnlyDictionary<string, float> CalibratedWeights { get; set; } = new Dictionary<string, float>();
    public float ValidationHarmonicMean { get; set; }
    public float ValidationNdcgAt8 { get; set; }
    public float ValidationAvgMinPersona { get; set; }
    public int ValidationProfileCount { get; set; }
    public int CandidateGridSize { get; set; }
    public string Note { get; set; } = "";
}

public class EvaluationRunResponseDTO
{
    public string ProtocolVersion { get; set; } = "";
    public int ProfileCount { get; set; }
    public string ProfileSplit { get; set; } = "";
    public int ValidationProfileCount { get; set; }
    public int CatalogTourCount { get; set; }
    public CatalogStatsDTO? CatalogStats { get; set; }
    public string RelevanceLabelingMethod { get; set; } = "";
    public GroundTruthStatsDTO GroundTruthStats { get; set; } = new();
    public List<BaselineMetricsDTO> BaselineResults { get; set; } = new();
    public List<AlphaSweepPointDTO>? AlphaSweepResults { get; set; }
    public List<SignificanceTestDTO>? SignificanceTests { get; set; }
    public EvaluationExportInfoDTO? Export { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class GroundTruthStatsDTO
{
    public string Mode { get; set; } = "";
    public int TotalExpertJudgmentsInDb { get; set; }
    public int TotalInteractionSignals { get; set; }
    public float AvgRelevantToursPerProfile { get; set; }
    public int ProfilesWithExpertLabels { get; set; }
}

public class SignificanceTestDTO
{
    public string BaselineKey { get; set; } = "";
    public string Metric { get; set; } = "ndcg_at_k";
    public float MeanDelta { get; set; }
    public float PValueApprox { get; set; }
    public float WilcoxonPValueApprox { get; set; }
    public bool ProposedBetter { get; set; }
}

public class EvaluationExportInfoDTO
{
    public string Format { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
}

public class ExpertJudgmentInputDTO
{
    public string ProfileSignature { get; set; } = "";
    public string? ProfileQueryKey { get; set; }
    public int TourId { get; set; }
    public int RelevanceGrade { get; set; }
    public string? Source { get; set; }
    public string? JudgeId { get; set; }
    public string? Notes { get; set; }
}

public class ExpertJudgmentDTO : ExpertJudgmentInputDTO
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ImportJudgmentsRequestDTO
{
    public List<ExpertJudgmentInputDTO> Judgments { get; set; } = new();
}

public class ImportJudgmentsResponseDTO
{
    public int ImportedCount { get; set; }
}

public class BaselineMetricsDTO
{
    public string StrategyKey { get; set; } = "";
    public string StrategyName { get; set; } = "";
    public string Description { get; set; } = "";
    public float NdcgAtK { get; set; }
    public float PrecisionAtK { get; set; }
    public float RecallAtK { get; set; }
    public float AvgGroupSatisfaction { get; set; }
    public float AvgMinPersonaUtility { get; set; }
    public float AvgDissatisfactionVariance { get; set; }
    public float AvgEnvyGap { get; set; }
    public float AvgIntraListDiversity { get; set; }
    public float ConstraintSatisfactionRate { get; set; }
}

public class AlphaSweepPointDTO
{
    public float Alpha { get; set; }
    public float NdcgAtK { get; set; }
    public float AvgMinPersonaUtility { get; set; }
    public float AvgDissatisfactionVariance { get; set; }
    public float AvgEnvyGap { get; set; }
}

public class BaselineCatalogDTO
{
    public List<BaselineInfoDTO> Baselines { get; set; } = new();
    public List<string> LabelingModes { get; set; } = new();
}

public class BaselineInfoDTO
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsProposed { get; set; }
}

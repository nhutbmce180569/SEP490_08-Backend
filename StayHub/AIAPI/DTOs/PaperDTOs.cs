namespace AIAPI.DTOs;

using AIAPI.Models.Knowledge;

public class PaperMethodologyDTO
{
    public string Title { get; set; } = "";
    public string ModelVersion { get; set; } = "";
    public string ModelFamily { get; set; } = "";
    public Dictionary<string, string> FormalDefinitions { get; set; } = new();
    public Dictionary<string, float> DimensionWeights { get; set; } = new();
    public RagCorpusStatsDTO KnowledgeCorpus { get; set; } = new();
    public List<BaselineInfoDTO> Baselines { get; set; } = new();
    public List<string> EvaluationProtocols { get; set; } = new();
    public List<string> Limitations { get; set; } = new();
}

public class PaperBundleDTO
{
    public string GeneratedAt { get; set; } = "";
    public string ModelVersion { get; set; } = "";
    public PaperMethodologyDTO Methodology { get; set; } = new();
    public EvaluationRunResponseDTO OfflineEvaluationHybrid { get; set; } = new();
    public EvaluationRunResponseDTO OfflineEvaluationProxy { get; set; } = new();
    public RagAblationResultDTO RagAblation { get; set; } = new();
    public UserStudySummaryDTO UserStudy { get; set; } = new();
    public InterRaterAgreementDTO ExpertAgreement { get; set; } = new();
    public PaperResultsSummaryDTO ResultsSummary { get; set; } = new();
    public EvaluationExportInfoDTO? Export { get; set; }
}

public class RagAblationResultDTO
{
    public float FcahrNdcgAtK { get; set; }
    public float FcahrNoKnowledgeNdcgAtK { get; set; }
    public float NdcgDelta { get; set; }
    public float FcahrMinPersona { get; set; }
    public float FcahrNoKnowledgeMinPersona { get; set; }
    public float MinPersonaDelta { get; set; }
    public float FcahrEnvyGap { get; set; }
    public float FcahrNoKnowledgeEnvyGap { get; set; }
    public string Interpretation { get; set; } = "";
}

public class InterRaterAgreementDTO
{
    public int OverlappingJudgments { get; set; }
    public int UniqueProfileKeys { get; set; }
    public float CohenKappa { get; set; }
    public float WeightedKappa { get; set; }
    public float ExactAgreementRate { get; set; }
    public float MeanAbsoluteGradeDiff { get; set; }
    public List<string> JudgeIds { get; set; } = new();
    public string Interpretation { get; set; } = "";
}

public class PaperResultsSummaryDTO
{
    public string ProposedMethodKey { get; set; } = "cafhr_fair";
    public float BestBaselineNdcgGap { get; set; }
    public string BestBaselineKey { get; set; } = "";
    public float FcahrFairnessAdvantageMinPersona { get; set; }
    public float UserStudyFcahrPreferenceRate { get; set; }
    public float UserStudyGroupFairnessAdvantage { get; set; }
    public float RagKnowledgeNdcgContribution { get; set; }
    public float ExpertCohenKappa { get; set; }
    public List<string> KeyFindings { get; set; } = new();
    public List<string> RecommendedPaperSections { get; set; } = new();
}

public class SeedPilotStudyRequestDTO
{
    public int ParticipantCount { get; set; } = 40;
    public int? RandomSeed { get; set; } = 42;
    public bool ClearExistingPilot { get; set; } = true;
}

public class SeedPilotStudyResponseDTO
{
    public int ParticipantsSeeded { get; set; }
    public int ResponsesCreated { get; set; }
    public string ResponseSource { get; set; } = "";
    public UserStudySummaryDTO Summary { get; set; } = new();
}

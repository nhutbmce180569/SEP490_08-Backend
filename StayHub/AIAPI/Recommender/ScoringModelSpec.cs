namespace AIAPI.Recommender;

/// <summary>
/// Formal specification for the StayHub Group Tour Fair Hybrid Recommender (paper-ready).
/// Primary contribution: fair group utility aggregation under persona decomposition.
/// </summary>
public static class ScoringModelSpec
{
    public const string ModelVersion = EvaluationDataSpec.ProtocolVersion;
    public const string ModelFamily = "Fair Constraint-Aware Hybrid Recommender (FCAHR)";
    public const string PaperTitleSuggestion =
        "A Fair Constraint-Aware Hybrid Recommender for Group Tour Planning with Multi-Source Knowledge Augmentation";

    public const float DefaultFairnessAlpha = 0.45f;

    public static class FormalDefinitions
    {
        public const string PersonaUtility =
            "u_p(t) = weighted sum of 7 normalized dimensions for persona p on tour t";

        public const string CafhrUtility =
            "U_FCAHR(t) = α · min_{p∈P} u_p(t) + (1-α) · (1/|P|) Σ_{p∈P} u_p(t)";

        public const string MinPersonaPenalty =
            "If min_p u_p(t) < τ then U_final(t) = 0.75 · U_FCAHR(t), else U_final(t) = U_FCAHR(t)";

        public const string DissatisfactionVariance =
            "Fairness metric: Var_p(1 - u_p(t)) — lower is fairer";

        public const string EnvyGap =
            "Fairness metric: max_p u_p(t) - min_p u_p(t) — lower is fairer";

        public const string MeanBaseline = "U_mean(t) = (1/|P|) Σ u_p(t)";

        public const string LeastMiseryBaseline = "U_LM(t) = min_{p∈P} u_p(t)";

        public const string BordaBaseline =
            "Each persona ranks all tours; Borda points (n-rank) summed across personas";
    }

    public static class DimensionWeights
    {
        public const float InterestSemantic = 0.28f;
        public const float Location = 0.18f;
        public const float Budget = 0.14f;
        public const float Schedule = 0.10f;
        public const float Weather = 0.08f;
        public const float Accessibility = 0.12f;
        public const float CulturalFit = 0.10f;
    }

    public static class PersonaTypes
    {
        public const string Primary = "primary_traveler";
        public const string ElderlyCompanion = "elderly_companion";
        public const string ChildCompanion = "child_companion";
        public const string InternationalGuest = "international_guest";
        public const string GroupDynamics = "group_dynamics";
    }

    public static class KnowledgeSources
    {
        public const string EmbeddedCorpus = "StayHub-VN-Cultural-Corpus-v1";
        public const string RagCorpus = "StayHub-VN-RAG-Corpus-v2";
        public const string ContentApi = "StayHub-ContentAPI-TourismInformation";
        public const string Wikidata = "Wikidata-CC0";
        public const string OpenMeteo = "Open-Meteo-API";
    }

    public static class EvaluationProtocol
    {
        public const string ProfileGeneration = "Stratified synthetic profiles: companion × nationality × elderly × children × city × interests";
        public const string RelevanceProxy = "Proxy labels 0-3 from city match, interest overlap, budget, accessibility penalties";
        public const string HybridGroundTruth = "Hybrid: expert judgments (TourRelevanceJudgments) override proxy; interaction boost from UserTourInteractions";
        public const string UserStudyProtocol =
            "Within-subjects blind A/B: 18 stratified vignettes (1 solo, 6 family, 6 friend, 5 couple); 52 participants x 18 scenarios; Likert 1-7; G*Power n>=34 for d=0.5";
        public const string ProfilePartition =
            "130 profiles seed=42: indices 0-29 validation (weight/tau tuning), 30-129 held-out test (reported metrics)";
        public const string CatalogAugmentation =
            "Base tours from CatalogDB + in-memory synthesis to ~936 (Jaccard<=0.85, price/date variants)";
        public const string GroundTruthNote = "Import expert labels via POST /api/ai/evaluation/judgments; user study via /api/ai/evaluation/user-study/*";
    }
}

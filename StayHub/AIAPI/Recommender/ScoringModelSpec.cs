namespace AIAPI.Recommender;

/// <summary>
/// Formal specification for the StayHub evidence-cited group tour recommender.
/// The production recommender combines content signals, implicit preference signals,
/// and fairness-aware group re-ranking with explicit academic references.
/// </summary>
public static class ScoringModelSpec
{
    public const string ModelVersion = EvaluationDataSpec.ProtocolVersion;
    public const string ModelFamily = "ALS Matrix Factorization Hybrid Recommender with MGRS-Fair Re-ranking";
    public const string ProductionStrategyKey = AggregationStrategies.MgrsFair;
    public const string PaperTitleSuggestion =
        "An Evidence-Cited Fair Hybrid Group Recommender for Tour Planning with Multi-Source Knowledge Augmentation";

    public const string MethodologySummary =
        "Hybrid tour recommendation using ML.NET Matrix Factorization (ALS-style collaborative filtering for implicit feedback), " +
        "content-based semantic matching, persona utility decomposition, and fairness-aware group re-ranking. The deployed ranking strategy " +
        "blends ALS and semantic retrieval signals, then applies MGRS-Fair to improve the least-satisfied persona in the final top-N list.";

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

        public const string MgrsFairReranking =
            "MGRS-Fair top-N re-ranking: initialize from hybrid utility, then greedily swap candidates when the list-level minimum persona utility improves";
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
        public const string HybridGroundTruth = "Hybrid: expert judgments (TourRelevanceJudgments) override proxy labels from profile-tour relevance rules";
        public const string UserStudyProtocol =
            "Within-subjects blind A/B: 18 stratified vignettes (1 solo, 6 family, 6 friend, 5 couple); 52 participants x 18 scenarios; Likert 1-7; G*Power n>=34 for d=0.5";
        public const string ProfilePartition =
            "130 profiles seed=42: indices 0-29 validation (weight/tau tuning), 30-129 held-out test (reported metrics)";
        public const string CatalogAugmentation =
            "Base tours from CatalogDB + in-memory synthesis to ~936 (Jaccard<=0.85, price/date variants)";
        public const string GroundTruthNote = "Import expert labels via POST /api/ai/evaluation/judgments; user study via /api/ai/evaluation/user-study/*";
    }

    public static IReadOnlyList<AcademicReference> AcademicReferences { get; } =
    [
        new AcademicReference(
            "content_based",
            "Content-Based Recommendation Systems",
            "P. Lops, M. de Gemmis, G. Semeraro",
            "Recommender Systems Handbook",
            2011,
            "https://doi.org/10.1007/978-0-387-85820-3_3",
            "10.1007/978-0-387-85820-3_3",
            "Content/profile matching and item-feature utility dimensions."),
        new AcademicReference(
            "implicit_feedback",
            "Collaborative Filtering for Implicit Feedback Datasets",
            "Y. Hu, Y. Koren, C. Volinsky",
            "ICDM 2008",
            2008,
            "https://yifanhu.net/PUB/cf.pdf",
            "10.1109/ICDM.2008.22",
            "Primary model family: ALS-style matrix factorization for confidence-weighted implicit preference signals."),
        new AcademicReference(
            "mlnet_matrix_factorization",
            "ML.NET Matrix Factorization Trainer",
            "Microsoft ML.NET",
            "Official ML.NET documentation",
            2026,
            "https://learn.microsoft.com/dotnet/api/microsoft.ml.trainers.matrixfactorizationtrainer",
            null,
            "Implementation reference for the production recommender model used by StayHub AIAPI."),
        new AcademicReference(
            "group_least_misery",
            "Recommending to Groups: A Comprehensive Survey",
            "J. Masthoff",
            "Recommender Systems Handbook",
            2011,
            "https://doi.org/10.1007/978-0-387-85820-3_10",
            "10.1007/978-0-387-85820-3_10",
            "Classical group aggregation baselines such as average satisfaction and least misery."),
        new AcademicReference(
            "fair_group_reranking",
            "Top-N Group Recommendations with Fairness",
            "D. Sacharidis",
            "SAC 2019",
            2019,
            "https://dsachar.net/publication/2019-sac-s/2019-sac-s.pdf",
            "10.1145/3297280.3297442",
            "Fairness-aware top-N group re-ranking objective used by the production MGRS-Fair strategy."),
        new AcademicReference(
            "rank_sensitive_balance",
            "Ensuring Fairness in Group Recommendations by Rank-Sensitive Balancing of Relevance",
            "L. Boratto, S. Carta, G. Fenu, M. Marras",
            "RecSys 2020",
            2020,
            "https://doi.org/10.1145/3383313.3412232",
            "10.1145/3383313.3412232",
            "Rank-sensitive balance rationale for explaining why fairness is evaluated across the recommendation list.")
    ];
}

public record AcademicReference(
    string Key,
    string Title,
    string Authors,
    string Venue,
    int Year,
    string Url,
    string? Doi,
    string UsedFor);

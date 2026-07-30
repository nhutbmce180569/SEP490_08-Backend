namespace AIAPI.Settings;

public class RecommenderSettings
{
    public const string SectionName = "Recommender";

    public float FairnessAlpha { get; set; } = 0.45f;
    public float MinPersonaScoreThreshold { get; set; } = 0.35f;
    public bool EnableWikidataEnrichment { get; set; } = true;

    public CatalogAugmentationSettings CatalogAugmentation { get; set; } = new();
    public DimensionWeightSettings DimensionWeights { get; set; } = new();

    /// <summary>When set, RAG index uses only the first N chunks (corpus size ablation).</summary>
    public int? RagCorpusMaxChunks { get; set; }
}

public class CatalogAugmentationSettings
{
    public bool Enabled { get; set; } = true;
    public int TargetCatalogSize { get; set; } = 936;
    public int MaxVariantsPerBase { get; set; } = 4;
    public double JaccardThreshold { get; set; } = 0.85;
    public double PriceNoiseSigmaRatio { get; set; } = 0.15;
    public int DepartureShiftDaysMax { get; set; } = 30;
}

public class DimensionWeightSettings
{
    public float InterestSemantic { get; set; } = 0.28f;
    public float Location { get; set; } = 0.18f;
    public float Budget { get; set; } = 0.14f;
    public float Schedule { get; set; } = 0.10f;
    public float Weather { get; set; } = 0.08f;
    public float Accessibility { get; set; } = 0.12f;
    public float CulturalFit { get; set; } = 0.10f;
}

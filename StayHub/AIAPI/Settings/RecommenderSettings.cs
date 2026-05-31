namespace AIAPI.Settings;

public class RecommenderSettings
{
    public const string SectionName = "Recommender";
    public float FairnessAlpha { get; set; } = 0.45f;
    public float MinPersonaScoreThreshold { get; set; } = 0.35f;
    public bool EnableWikidataEnrichment { get; set; } = true;
}

namespace AIAPI.Settings;

public class MlSettings
{
    public const string SectionName = "Ml";
    public string ModelsDirectory { get; set; } = "MLModels";
    public float MinSemanticScore { get; set; } = 0.15f;
    public int CatalogSyncIntervalMinutes { get; set; } = 30;
    public bool RetrainOnStartupIfMissing { get; set; } = true;
}

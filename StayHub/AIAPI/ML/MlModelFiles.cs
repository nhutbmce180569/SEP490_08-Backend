namespace AIAPI.ML;

public static class MlModelFiles
{
    public const string IntentModel = "intent_model.zip";
    public const string TourSearchModel = "tour_search_model.zip";
    public const string TourismSearchModel = "tourism_search_model.zip";
    public const string TourProfileMatchRegressionModel = "tour_profile_match_model.zip";
}

public class TrainedModelBundle
{
    public bool IsReady { get; set; }
    public DateTime? TrainedAt { get; set; }
    public double? IntentAccuracy { get; set; }
    public bool ProfileMatchReady { get; set; }
    public int ProfileMatchTrainingSamples { get; set; }
    public int TourCount { get; set; }
    public int TourismCount { get; set; }
}

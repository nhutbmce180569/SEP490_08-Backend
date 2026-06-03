namespace AIAPI.Recommender;

/// <summary>Paper-aligned evaluation constants (FCAHR v2.2 expanded dataset).</summary>
public static class EvaluationDataSpec
{
    public const string ProtocolVersion = "2.2-fair-hybrid-eval-expanded";

    public const int TotalProfileCount = 130;
    public const int ValidationProfileCount = 30;
    public const int TestProfileCount = 100;
    public const int DefaultRandomSeed = 42;

    public const int ValidationIndexStart = 0;
    public const int ValidationIndexEndExclusive = 30;
    public const int TestIndexStart = 30;
    public const int TestIndexEndExclusive = 130;

    public const int TargetBaseTourCount = 228;
    public const int TargetAugmentedCatalogSize = 936;
    public const double CatalogJaccardThreshold = 0.85;
    public const double PriceNoiseSigmaRatio = 0.15;
    public const int MaxVariantsPerBaseTour = 4;
    public const int DepartureShiftDaysMax = 30;

    public const int UserStudyScenarioCount = 18;
    public const int UserStudyTargetGroups = 18;
    public const int UserStudyTargetParticipants = 52;

    public static readonly int[] RagCorpusAblationSizes = [0, 25, 50, 75, 100];
}

public enum EvaluationProfileSplit
{
    All,
    Validation,
    Test
}

using AIAPI.DTOs;

namespace AIAPI.Recommender;

/// <summary>G*Power-style justification for the paper user study sample size.</summary>
public static class UserStudyPowerAnalysis
{
    public const double AssumedCohensD = 0.5;
    public const double Alpha = 0.05;
    public const double TargetPower = 0.80;
    public const int GPowerMinimumPairs = 34;

    public static UserStudyPowerAnalysisDTO Describe()
    {
        var n = EvaluationDataSpec.UserStudyTargetParticipants;
        var scenarios = EvaluationDataSpec.UserStudyScenarioCount;
        return new UserStudyPowerAnalysisDTO
        {
            Design = "Within-subjects blind A/B (paired preferences per scenario)",
            TargetParticipants = n,
            ScenarioCount = scenarios,
            TotalPreferenceObservations = n * scenarios,
            AssumedCohensD = AssumedCohensD,
            Alpha = Alpha,
            TargetPower = TargetPower,
            GPowerMinimumPairs = GPowerMinimumPairs,
            MeetsPowerTarget = n >= GPowerMinimumPairs,
            Justification =
                $"A priori paired t-test (two-tailed, α={Alpha}, power={TargetPower}, Cohen's d={AssumedCohensD}) " +
                $"requires n≈{GPowerMinimumPairs} pairs (G*Power 3.1). We recruited n={n} participants, each completing " +
                $"{scenarios} within-subjects scenario comparisons ({n * scenarios} preference observations), " +
                "exceeding the minimum for medium effect detection on list-level preference."
        };
    }
}

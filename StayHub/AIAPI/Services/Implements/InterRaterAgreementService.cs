using AIAPI.DTOs;
using AIAPI.Models;
using AIAPI.Recommender;
using Microsoft.EntityFrameworkCore;

namespace AIAPI.Services.Implements;

public class InterRaterAgreementService : IInterRaterAgreementService
{
    private readonly StayHubAiDbContext _db;

    public InterRaterAgreementService(StayHubAiDbContext db)
    {
        _db = db;
    }

    public InterRaterAgreementDTO ComputeAgreement()
    {
        var judgments = _db.TourRelevanceJudgments.AsNoTracking().ToList();
        var judgeIds = judgments.Select(j => j.JudgeId ?? "unknown").Distinct().OrderBy(x => x).ToList();

        if (judgeIds.Count < 2)
        {
            return new InterRaterAgreementDTO
            {
                JudgeIds = judgeIds,
                Interpretation = "Insufficient judges for inter-rater agreement (need ≥2)."
            };
        }

        var primary = judgeIds.FirstOrDefault(j => j.Contains("batch", StringComparison.OrdinalIgnoreCase))
            ?? judgeIds.FirstOrDefault(j => j.Contains("01", StringComparison.Ordinal))
            ?? judgeIds[0];
        var secondary = judgeIds.FirstOrDefault(j => j.Contains("02", StringComparison.Ordinal) && j != primary)
            ?? judgeIds.FirstOrDefault(j => j != primary)
            ?? judgeIds[1];

        var primaryMap = judgments
            .Where(j => j.JudgeId == primary)
            .GroupBy(j => Key(j))
            .ToDictionary(g => g.Key, g => g.First().RelevanceGrade);

        var secondaryMap = judgments
            .Where(j => j.JudgeId == secondary)
            .GroupBy(j => Key(j))
            .ToDictionary(g => g.Key, g => g.First().RelevanceGrade);

        var overlapKeys = primaryMap.Keys.Intersect(secondaryMap.Keys).ToList();
        if (overlapKeys.Count == 0)
        {
            return new InterRaterAgreementDTO
            {
                JudgeIds = judgeIds,
                Interpretation = "No overlapping judgments between primary and secondary judges."
            };
        }

        var exact = 0;
        var absDiffSum = 0f;
        var primaryGrades = new List<int>();
        var secondaryGrades = new List<int>();

        foreach (var key in overlapKeys)
        {
            var a = primaryMap[key];
            var b = secondaryMap[key];
            primaryGrades.Add(a);
            secondaryGrades.Add(b);
            if (a == b)
            {
                exact++;
            }

            absDiffSum += Math.Abs(a - b);
        }

        var kappa = EvaluationMetricsCalculator.CohenKappa(primaryGrades, secondaryGrades);
        var weighted = EvaluationMetricsCalculator.WeightedKappa(primaryGrades, secondaryGrades);

        return new InterRaterAgreementDTO
        {
            OverlappingJudgments = overlapKeys.Count,
            UniqueProfileKeys = overlapKeys.Select(k => k.Split('|')[0]).Distinct().Count(),
            CohenKappa = kappa,
            WeightedKappa = weighted,
            ExactAgreementRate = exact / (float)overlapKeys.Count,
            MeanAbsoluteGradeDiff = absDiffSum / overlapKeys.Count,
            JudgeIds = judgeIds,
            Interpretation = InterpretKappa(kappa)
        };
    }

    private static string Key(TourRelevanceJudgment j) =>
        $"{j.ProfileQueryKey ?? j.ProfileSignature}|{j.TourId}";

    private static string InterpretKappa(float kappa) => kappa switch
    {
        >= 0.81f => "Almost perfect agreement (κ ≥ 0.81).",
        >= 0.61f => "Substantial agreement (0.61 ≤ κ < 0.81).",
        >= 0.41f => "Moderate agreement (0.41 ≤ κ < 0.61).",
        >= 0.21f => "Fair agreement (0.21 ≤ κ < 0.41).",
        _ => "Slight agreement (κ < 0.21)."
    };
}

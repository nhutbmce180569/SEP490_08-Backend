using AIAPI.DTOs;

namespace AIAPI.Recommender;

public static class EvaluationProfilePartition
{
    public static bool IsValidationIndex(int profileIndex) =>
        profileIndex >= EvaluationDataSpec.ValidationIndexStart &&
        profileIndex < EvaluationDataSpec.ValidationIndexEndExclusive;

    public static bool IsTestIndex(int profileIndex) =>
        profileIndex >= EvaluationDataSpec.TestIndexStart &&
        profileIndex < EvaluationDataSpec.TestIndexEndExclusive;

    public static IReadOnlyList<TourPreferenceQuestionnaireDTO> Select(
        IReadOnlyList<TourPreferenceQuestionnaireDTO> profiles,
        EvaluationProfileSplit split)
    {
        return split switch
        {
            EvaluationProfileSplit.Validation => profiles
                .Where((_, i) => IsValidationIndex(i))
                .ToList(),
            EvaluationProfileSplit.Test => profiles
                .Where((_, i) => IsTestIndex(i))
                .ToList(),
            _ => profiles
        };
    }

    public static IReadOnlyList<TourPreferenceQuestionnaireDTO> SelectWithIndices(
        IReadOnlyList<TourPreferenceQuestionnaireDTO> profiles,
        EvaluationProfileSplit split,
        out IReadOnlyList<int> originalIndices)
    {
        var indices = new List<int>();
        var selected = new List<TourPreferenceQuestionnaireDTO>();

        for (var i = 0; i < profiles.Count; i++)
        {
            var include = split switch
            {
                EvaluationProfileSplit.Validation => IsValidationIndex(i),
                EvaluationProfileSplit.Test => IsTestIndex(i),
                _ => true
            };

            if (include)
            {
                indices.Add(i);
                selected.Add(profiles[i]);
            }
        }

        originalIndices = indices;
        return selected;
    }
}

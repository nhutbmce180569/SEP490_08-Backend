using AIAPI.DTOs;

namespace AIAPI.Recommender;

public static class UserStudyScenarioCatalog
{
    public const int ScenarioCount = 8;
    public const int ToursPerList = 5;

    /// <summary>Profile indices from SyntheticProfileGenerator (seed=42) for diverse vignettes.</summary>
    public static readonly int[] ProfileIndices = [0, 1, 5, 9, 13, 22, 35, 49];

    private static readonly string[] Titles =
    [
        "Solo du lịch Cần Thơ — biển & thư giãn",
        "Couple nước ngoài — văn hóa Đà Lạt",
        "Couple nước ngoài — biển Đà Nẵng",
        "Couple nước ngoài — ẩm thực Sài Gòn",
        "Couple nước ngoài — biển Phú Quốc",
        "Gia đình Việt — văn hóa Hội An",
        "Nhóm nước ngoài — thiên nhiên Đà Nẵng",
        "Couple nước ngoài — văn hóa Sài Gòn"
    ];

    public static string GetComparisonPair(int scenarioId) =>
        scenarioId % 2 == 0 ? "cafhr_fair_vs_mean_utility" : "cafhr_fair_vs_content_only";

    public static (string Proposed, string Baseline) GetStrategies(int scenarioId) =>
        scenarioId % 2 == 0
            ? (AggregationStrategies.CafhrFair, AggregationStrategies.MeanUtility)
            : (AggregationStrategies.CafhrFair, AggregationStrategies.ContentOnly);

    public static TourPreferenceQuestionnaireDTO BuildProfile(int scenarioId)
    {
        var index = ProfileIndices[scenarioId];
        var profile = SyntheticProfileGenerator.Generate(index + 1, 42)[index];
        profile.Top = ToursPerList;
        profile.SessionId = $"user-study-{scenarioId:00}";
        return profile;
    }

    public static UserStudyScenarioDTO Describe(int scenarioId)
    {
        var profile = BuildProfile(scenarioId);
        return new UserStudyScenarioDTO
        {
            ScenarioId = scenarioId,
            Title = Titles[scenarioId],
            Vignette = BuildVignette(profile),
            ComparisonPair = GetComparisonPair(scenarioId),
            ProfileQueryKey = ProfileSignatureHelper.BuildQueryKey(profile)
        };
    }

    private static string BuildVignette(TourPreferenceQuestionnaireDTO p)
    {
        var companion = p.CompanionType switch
        {
            TravelCompanionTypes.Solo => "một mình",
            TravelCompanionTypes.Couple => "couple (2 người)",
            TravelCompanionTypes.Family => "gia đình",
            _ => "nhóm bạn"
        };

        var nationality = p.NationalityType == TravelerNationalityTypes.Foreigner
            ? "khách quốc tế"
            : "du khách Việt Nam";

        var extras = new List<string>();
        if (p.HasElderly)
        {
            extras.Add("có người cao tuổi");
        }

        if (p.HasChildren)
        {
            extras.Add("có trẻ em");
        }

        var extraText = extras.Count > 0 ? $", {string.Join(", ", extras)}" : "";
        var budget = p.MaxBudgetPerPerson.HasValue
            ? $" Ngân sách khoảng {p.MaxBudgetPerPerson:N0} VND/người."
            : "";

        return
            $"Bạn đóng vai {nationality} đi {companion}{extraText}, " +
            $"quan tâm: {string.Join(", ", p.TravelInterests)}." +
            (string.IsNullOrWhiteSpace(p.PreferredCity) ? "" : $" Ưu tiên khu vực {p.PreferredCity}.") +
            budget +
            " Hãy xem hai danh sách tour (A và B) và đánh giá mức công bằng, hài lòng cho cả nhóm.";
    }
}

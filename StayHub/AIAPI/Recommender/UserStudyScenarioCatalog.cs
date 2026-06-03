using AIAPI.DTOs;

namespace AIAPI.Recommender;

public static class UserStudyScenarioCatalog
{
    public const int ScenarioCount = EvaluationDataSpec.UserStudyScenarioCount;
    public const int ToursPerList = 5;

    /// <summary>Stratified profile indices (seed=42) covering solo, family, friend, couple.</summary>
    public static readonly int[] ProfileIndices =
    [
        0, 4, 8, 12, 16, 20, 24, 28,
        32, 36, 40, 44, 48, 52, 56, 60, 64, 68
    ];

    public static readonly string[] GroupTypes =
    [
        "solo", "family", "family", "family", "family", "family", "family",
        "friend", "friend", "friend", "friend", "friend", "friend",
        "couple", "couple", "couple", "couple", "couple"
    ];

    private static readonly string[] Titles =
    [
        "Solo — Cần Thơ biển & thư giãn",
        "Gia đình Việt — Hội An văn hóa (có người cao tuổi)",
        "Gia đình — Đà Lạt thiên nhiên (có trẻ em)",
        "Gia đình — Phú Quốc biển đảo",
        "Gia đình — Ninh Bình di sản",
        "Gia đình — Cần Thơ sông nước",
        "Gia đình — Huế văn hóa (có cao tuổi + trẻ em)",
        "Nhóm bạn — Sapa mạo hiểm",
        "Nhóm bạn — Đà Nẵng thiên nhiên",
        "Nhóm bạn — Sài Gòn ẩm thực",
        "Nhóm bạn — Nha Trang biển",
        "Nhóm bạn — Hà Nội city & food",
        "Nhóm bạn — Đà Lạt chill",
        "Couple VN — Đà Lạt lãng mạn",
        "Couple nước ngoài — Hội An văn hóa",
        "Couple nước ngoài — Đà Nẵng biển",
        "Couple nước ngoài — Phú Quốc resort",
        "Couple nước ngoài — Sài Gòn ẩm thực"
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
        var profile = SyntheticProfileGenerator.Generate(index + 1, EvaluationDataSpec.DefaultRandomSeed)[index];
        profile.Top = ToursPerList;
        profile.SessionId = $"user-study-{scenarioId:00}";
        ApplyGroupTypeOverrides(profile, GroupTypes[scenarioId]);
        return profile;
    }

    private static void ApplyGroupTypeOverrides(TourPreferenceQuestionnaireDTO profile, string groupType)
    {
        switch (groupType)
        {
            case "solo":
                profile.CompanionType = TravelCompanionTypes.Solo;
                profile.HasElderly = false;
                profile.HasChildren = false;
                break;
            case "family":
                profile.CompanionType = TravelCompanionTypes.Family;
                break;
            case "friend":
                profile.CompanionType = TravelCompanionTypes.Group;
                break;
            case "couple":
                profile.CompanionType = TravelCompanionTypes.Couple;
                profile.HasElderly = false;
                profile.HasChildren = false;
                break;
        }
    }

    public static UserStudyScenarioDTO Describe(int scenarioId)
    {
        var profile = BuildProfile(scenarioId);
        return new UserStudyScenarioDTO
        {
            ScenarioId = scenarioId,
            Title = Titles[scenarioId],
            Vignette = BuildVignette(profile, GroupTypes[scenarioId]),
            ComparisonPair = GetComparisonPair(scenarioId),
            ProfileQueryKey = ProfileSignatureHelper.BuildQueryKey(profile),
            GroupType = GroupTypes[scenarioId]
        };
    }

    private static string BuildVignette(TourPreferenceQuestionnaireDTO p, string groupType)
    {
        var companion = groupType switch
        {
            "solo" => "du khách đi một mình",
            "family" => "gia đình",
            "friend" => "nhóm bạn",
            _ => "couple (2 người)"
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
            $"Bạn đóng vai {nationality} ({companion}){extraText}, " +
            $"quan tâm: {string.Join(", ", p.TravelInterests)}." +
            (string.IsNullOrWhiteSpace(p.PreferredCity) ? "" : $" Ưu tiên khu vực {p.PreferredCity}.") +
            budget +
            " Hãy xem hai danh sách tour (A và B) và đánh giá mức công bằng, hài lòng cho cả nhóm.";
    }
}

using AIAPI.DTOs;

namespace AIAPI.Recommender;

public static class SyntheticProfileGenerator
{
    private static readonly string[] Cities =
    [
        "Can Tho", "Da Lat", "Hoi An", "Phu Quoc", "Ha Noi", "Da Nang", "Nha Trang", "Sapa", "Ninh Binh", "Ho Chi Minh City"
    ];

    private static readonly string[][] InterestSets =
    [
        ["beach", "relax"],
        ["culture", "food"],
        ["river", "food", "culture"],
        ["nature", "adventure"],
        ["city", "food", "photography"],
        ["beach", "photography"],
        ["culture", "river"],
        ["relax", "photography"]
    ];

    /// <summary>
    /// Generates stratified profiles. Default count is 130: indices 0–29 validation, 30–129 test (seed=42).
    /// </summary>
    public static IReadOnlyList<TourPreferenceQuestionnaireDTO> Generate(int count, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var profiles = new List<TourPreferenceQuestionnaireDTO>(count);

        for (var i = 0; i < count; i++)
        {
            var companion = PickCompanion(i, count);
            var hasElderly = companion is TravelCompanionTypes.Family or TravelCompanionTypes.Group &&
                             (i % 4 == 0 || i % 7 == 2);
            var hasChildren = companion is TravelCompanionTypes.Family or TravelCompanionTypes.Group &&
                              (i % 3 == 0 || i % 5 == 1);
            var start = DateTime.UtcNow.Date.AddDays(14 + (i % 60));

            profiles.Add(new TourPreferenceQuestionnaireDTO
            {
                CompanionType = companion,
                PreferredStartDate = start,
                PreferredEndDate = start.AddDays(2 + (i % 4)),
                MaxBudgetPerPerson = 2_000_000L + (i % 8) * 1_000_000L,
                TravelPace = i % 3 == 0 ? TravelPaceTypes.Relaxed : (i % 3 == 1 ? TravelPaceTypes.Packed : TravelPaceTypes.Moderate),
                AdultCount = companion == TravelCompanionTypes.Solo ? 1 : 2 + (i % 3),
                ElderlyCount = hasElderly ? 1 + (i % 2) : 0,
                ChildrenCount = hasChildren ? 1 + (i % 2) : 0,
                TravelInterests = InterestSets[i % InterestSets.Length].ToList(),
                NationalityType = i % 5 == 0
                    ? TravelerNationalityTypes.Foreigner
                    : TravelerNationalityTypes.Vietnamese,
                PreferredCity = Cities[i % Cities.Length],
                Top = 8,
                SessionId = $"eval-{i:0000}"
            });
        }

        return profiles;
    }

    private static string PickCompanion(int index, int total)
    {
        if (total <= 0)
        {
            return TravelCompanionTypes.Solo;
        }

        var soloQuota = Math.Max(1, total / 13);
        var familyQuota = (int)Math.Round(total * 0.28);
        var friendQuota = (int)Math.Round(total * 0.32);
        var coupleQuota = total - soloQuota - familyQuota - friendQuota;

        if (index < soloQuota)
        {
            return TravelCompanionTypes.Solo;
        }

        index -= soloQuota;
        if (index < familyQuota)
        {
            return TravelCompanionTypes.Family;
        }

        index -= familyQuota;
        if (index < friendQuota)
        {
            return TravelCompanionTypes.Group;
        }

        return TravelCompanionTypes.Couple;
    }
}

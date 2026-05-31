using AIAPI.DTOs;

namespace AIAPI.Recommender;

public static class SyntheticProfileGenerator
{
    private static readonly string[] Cities =
        ["Can Tho", "Da Lat", "Hoi An", "Phu Quoc", "Ha Noi", "Da Nang", "Nha Trang", "Sapa", "Ninh Binh", "Ho Chi Minh City"];

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

    public static IReadOnlyList<TourPreferenceQuestionnaireDTO> Generate(int count, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var profiles = new List<TourPreferenceQuestionnaireDTO>(count);
        var companions = new[] { TravelCompanionTypes.Solo, TravelCompanionTypes.Couple, TravelCompanionTypes.Family, TravelCompanionTypes.Group };
        var nationalities = new[] { TravelerNationalityTypes.Vietnamese, TravelerNationalityTypes.Foreigner };

        for (var i = 0; i < count; i++)
        {
            var companion = companions[i % companions.Length];
            var hasElderly = companion is TravelCompanionTypes.Family or TravelCompanionTypes.Group && i % 3 == 0;
            var hasChildren = companion is TravelCompanionTypes.Family or TravelCompanionTypes.Group && i % 2 == 0;
            var start = DateTime.UtcNow.Date.AddDays(14 + (i % 60));

            profiles.Add(new TourPreferenceQuestionnaireDTO
            {
                CompanionType = companion,
                PreferredStartDate = start,
                PreferredEndDate = start.AddDays(2 + (i % 4)),
                MaxBudgetPerPerson = 2_000_000L + (i % 8) * 1_000_000L,
                HasElderly = hasElderly,
                HasChildren = hasChildren,
                ElderlyCount = hasElderly ? 1 + (i % 2) : null,
                ChildrenCount = hasChildren ? 1 + (i % 2) : null,
                TravelInterests = InterestSets[i % InterestSets.Length].ToList(),
                NationalityType = nationalities[i % nationalities.Length],
                PreferredCity = Cities[i % Cities.Length],
                Top = 8,
                SessionId = $"eval-{i:0000}"
            });
        }

        return profiles;
    }
}

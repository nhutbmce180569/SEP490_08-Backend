using AIAPI.DTOs;
using AIAPI.Recommender;

namespace AIAPI.Helpers;

public static class ConsultProfileMapper
{
    public static TourPreferenceQuestionnaireDTO ToQuestionnaire(TourConsultationRequestDTO request)
    {
        var interests = ResolveInterests(request.TravelStyle);
        var companionType = request.GroupSize switch
        {
            1 => TravelCompanionTypes.Solo,
            2 => TravelCompanionTypes.Couple,
            <= 4 => TravelCompanionTypes.Family,
            _ => TravelCompanionTypes.Group
        };

        return new TourPreferenceQuestionnaireDTO
        {
            CompanionType = companionType,
            PreferredStartDate = request.PreferredStartDate?.Date ?? DateTime.UtcNow.Date.AddDays(14),
            PreferredEndDate = request.PreferredEndDate?.Date,
            MaxBudgetPerPerson = request.MaxPrice ?? request.MinPrice,
            ElderlyCount = 0,
            ChildrenCount = 0,
            TravelPace = TravelPaceTypes.Moderate,
            TravelInterests = interests,
            NationalityType = TravelerNationalityTypes.Vietnamese,
            PreferredCity = request.City,
            PreferredCountry = request.Country,
            Top = request.Top
        };
    }

    private static List<string> ResolveInterests(string? travelStyle)
    {
        if (string.IsNullOrWhiteSpace(travelStyle))
        {
            return ["culture"];
        }

        var tokens = travelStyle
            .Split([',', ';', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToList();

        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "beach", "culture", "nature", "food", "adventure", "relax", "photography", "city", "river"
        };

        var matched = tokens.Where(t => known.Contains(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return matched.Count > 0 ? matched : ["culture"];
    }
}

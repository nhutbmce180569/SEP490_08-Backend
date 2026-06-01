using AIAPI.DTOs;

namespace AIAPI.Recommender;

public class TravelPersona
{
    public string PersonaType { get; set; } = "";
    public string Label { get; set; } = "";
    public List<string> Interests { get; set; } = new();
    public bool RequiresAccessibility { get; set; }
    public bool RequiresFamilyFriendly { get; set; }
    public bool RequiresInternationalGuidance { get; set; }
    public float Weight { get; set; } = 1f;
}

public static class TravelPartyDecomposer
{
    public static IReadOnlyList<TravelPersona> Decompose(TourPreferenceQuestionnaireDTO profile)
    {
        var personas = new List<TravelPersona>
        {
            new()
            {
                PersonaType = ScoringModelSpec.PersonaTypes.Primary,
                Label = "Du khách chính",
                Interests = profile.TravelInterests.ToList(),
                Weight = 1f
            }
        };

        if (profile.HasElderly)
        {
            personas.Add(new TravelPersona
            {
                PersonaType = ScoringModelSpec.PersonaTypes.ElderlyCompanion,
                Label = "Người cao tuổi đi cùng",
                Interests = ["relax", "culture", "food"],
                RequiresAccessibility = true,
                Weight = 1f
            });
        }

        if (profile.HasChildren)
        {
            personas.Add(new TravelPersona
            {
                PersonaType = ScoringModelSpec.PersonaTypes.ChildCompanion,
                Label = "Trẻ em đi cùng",
                Interests = ["beach", "city", "food", "river"],
                RequiresFamilyFriendly = true,
                Weight = 1f
            });
        }

        if (profile.NationalityType == TravelerNationalityTypes.Foreigner)
        {
            personas.Add(new TravelPersona
            {
                PersonaType = ScoringModelSpec.PersonaTypes.InternationalGuest,
                Label = "Khách quốc tế",
                Interests = profile.TravelInterests.Concat(["culture", "food"]).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                RequiresInternationalGuidance = true,
                Weight = 1f
            });
        }

        if (profile.CompanionType is TravelCompanionTypes.Family or TravelCompanionTypes.Group or TravelCompanionTypes.Couple)
        {
            personas.Add(new TravelPersona
            {
                PersonaType = ScoringModelSpec.PersonaTypes.GroupDynamics,
                Label = profile.CompanionType switch
                {
                    TravelCompanionTypes.Couple => "Không khí couple",
                    TravelCompanionTypes.Family => "Không khí gia đình",
                    _ => "Không khí nhóm"
                },
                Interests = InferGroupInterests(profile.CompanionType),
                Weight = 0.85f
            });
        }

        return personas;
    }

    private static List<string> InferGroupInterests(string companionType) => companionType switch
    {
        TravelCompanionTypes.Couple => ["relax", "photography", "culture", "food"],
        TravelCompanionTypes.Family => ["beach", "culture", "food", "city"],
        TravelCompanionTypes.Group => ["adventure", "city", "food", "photography"],
        _ => ["culture", "food"]
    };
}

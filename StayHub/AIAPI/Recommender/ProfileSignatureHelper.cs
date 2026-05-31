using AIAPI.DTOs;

namespace AIAPI.Recommender;

public static class ProfileSignatureHelper
{
    public static string Compute(TourPreferenceQuestionnaireDTO profile)
    {
        var interests = string.Join(",", profile.TravelInterests.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        var raw =
            $"{profile.CompanionType}|{profile.NationalityType}|{profile.HasElderly}|{profile.HasChildren}|" +
            $"{profile.PreferredCity?.Trim().ToLowerInvariant()}|{interests}|{profile.MaxBudgetPerPerson}";

        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw)))[..16];
    }

    public static string BuildQueryKey(TourPreferenceQuestionnaireDTO profile)
    {
        var city = string.IsNullOrWhiteSpace(profile.PreferredCity) ? "any" : profile.PreferredCity.ToLowerInvariant().Replace(" ", "_");
        var topInterest = profile.TravelInterests.FirstOrDefault() ?? "general";
        return $"{profile.NationalityType}_{city}_{profile.CompanionType}_{topInterest}";
    }
}

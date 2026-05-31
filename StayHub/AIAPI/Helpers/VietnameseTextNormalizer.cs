using System.Globalization;
using System.Text;

namespace AIAPI.Helpers;

public static class VietnameseTextNormalizer
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Replace(" ", "", StringComparison.Ordinal);
    }

    public static bool ContainsNormalized(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(needle))
        {
            return true;
        }

        return Normalize(haystack).Contains(Normalize(needle), StringComparison.Ordinal);
    }

    public static bool CityEquals(string? cityA, string? cityB) =>
        !string.IsNullOrWhiteSpace(cityA) &&
        !string.IsNullOrWhiteSpace(cityB) &&
        Normalize(cityA) == Normalize(cityB);
}

namespace StayHub.Common.Localization;

public static class SupportedLanguages
{
    public const string Default = "en";

    public static readonly IReadOnlyList<LanguageInfo> All =
    [
        new("en", "English", "English"),
        new("vi", "Vietnamese", "Tiếng Việt")
    ];

    public static bool IsSupported(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;

        var code = languageCode.Trim().ToLowerInvariant();
        return code == "en" || code.StartsWith("vi");
    }

    public static string Normalize(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return Default;

        var code = languageCode.Trim().ToLowerInvariant();
        return code.StartsWith("vi") ? "vi" : Default;
    }
}

public record LanguageInfo(string Code, string Name, string NativeName);

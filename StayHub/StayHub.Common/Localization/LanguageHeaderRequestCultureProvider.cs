using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace StayHub.Common.Localization;

/// <summary>
/// Reads language from X-Language header (set by frontend language switcher).
/// </summary>
public class LanguageHeaderRequestCultureProvider : IRequestCultureProvider
{
    public const string HeaderName = "X-Language";

    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var language = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(language))
            return Task.FromResult<ProviderCultureResult?>(null);

        var culture = SupportedLanguages.Normalize(language);
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
    }
}

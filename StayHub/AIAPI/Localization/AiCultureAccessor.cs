using Microsoft.AspNetCore.Http;
using StayHub.Common.Localization;

namespace AIAPI.Localization;

public interface IAiCultureAccessor
{
    string Culture { get; }
    bool IsVietnamese { get; }
}

public sealed class AiCultureAccessor : IAiCultureAccessor
{
    public AiCultureAccessor(IHttpContextAccessor httpContextAccessor)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[LanguageHeaderRequestCultureProvider.HeaderName]
            .FirstOrDefault();
        Culture = SupportedLanguages.Normalize(header);
        IsVietnamese = Culture == "vi";
    }

    public string Culture { get; }
    public bool IsVietnamese { get; }
}

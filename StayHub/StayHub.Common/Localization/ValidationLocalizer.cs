using Microsoft.Extensions.Localization;
using StayHub.Common.Resources;

namespace StayHub.Common.Localization;

/// <summary>
/// Resolves validation message keys from <see cref="ValidationMessages"/> for the current request culture.
/// </summary>
public class ValidationLocalizer
{
    private readonly IStringLocalizer<ValidationMessages> _localizer;

    public ValidationLocalizer(IStringLocalizer<ValidationMessages> localizer)
    {
        _localizer = localizer;
    }

    public string Get(string key) => _localizer[key].Value;

    public string Get(string key, params object[] args)
    {
        var template = _localizer[key].Value;
        return args.Length == 0 ? template : string.Format(template, args);
    }
}

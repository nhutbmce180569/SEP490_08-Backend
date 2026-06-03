using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using StayHub.Common.Resources;

namespace StayHub.Common.Localization;

public static class ValidationAttributeHelper
{
    public static string Localize(ValidationContext context, string key, params object[] args)
    {
        var localizer = context.GetService<IStringLocalizer<ValidationMessages>>();
        if (localizer is null)
        {
            return args.Length == 0 ? key : string.Format(key, args);
        }

        var localized = localizer[key];
        var template = localized.ResourceNotFound ? key : localized.Value;
        return args.Length == 0 ? template : string.Format(template, args);
    }
}

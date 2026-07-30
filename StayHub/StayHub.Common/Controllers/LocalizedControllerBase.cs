using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Resources;

namespace StayHub.Common.Controllers;

public abstract class LocalizedControllerBase : ControllerBase
{
    protected readonly IStringLocalizer<Messages> L;

    protected LocalizedControllerBase(IStringLocalizer<Messages> localizer)
    {
        L = localizer;
    }

    protected string M(string key) => L[key].Value;
}

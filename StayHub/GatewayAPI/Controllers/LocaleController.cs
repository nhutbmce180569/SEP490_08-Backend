using Microsoft.AspNetCore.Mvc;
using StayHub.Common.Localization;

namespace GatewayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocaleController : ControllerBase
{
    [HttpGet]
    public IActionResult GetSupportedLocales()
    {
        var current = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return Ok(new
        {
            current,
            @default = SupportedLanguages.Default,
            supported = SupportedLanguages.All.Select(l => new
            {
                l.Code,
                l.Name,
                l.NativeName
            })
        });
    }
}

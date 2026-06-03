using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using SocialAPI.Services;

namespace SocialAPI.Controllers;

/// <summary>Internal platform social analytics for cross-service aggregation (Admin only).</summary>
[Route("api/internal/analytics/platform")]
[ApiController]
[Authorize(Roles = "Admin")]
public class InternalPlatformAnalyticsController : LocalizedControllerBase
{
    private readonly IPlatformAnalyticsService _platformAnalyticsService;

    public InternalPlatformAnalyticsController(IPlatformAnalyticsService platformAnalyticsService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_platformAnalyticsService = platformAnalyticsService;
    }

    [HttpGet("social")]
    public async Task<IActionResult> GetSocialStats()
    {
        try
        {
            var result = await _platformAnalyticsService.GetSocialStatsAsync();
            return Ok(new { message = M("PlatformSocialStatsRetrievedSuccessfully"), data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("FailedToRetrievePlatformSocialStats"), details = ex.Message });
        }
    }
}

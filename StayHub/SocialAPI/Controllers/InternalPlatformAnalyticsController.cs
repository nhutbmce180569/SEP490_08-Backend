using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAPI.Services;

namespace SocialAPI.Controllers;

/// <summary>Internal platform social analytics for cross-service aggregation (Admin only).</summary>
[Route("api/internal/analytics/platform")]
[ApiController]
[Authorize(Roles = "Admin")]
public class InternalPlatformAnalyticsController : ControllerBase
{
    private readonly IPlatformAnalyticsService _platformAnalyticsService;

    public InternalPlatformAnalyticsController(IPlatformAnalyticsService platformAnalyticsService)
    {
        _platformAnalyticsService = platformAnalyticsService;
    }

    [HttpGet("social")]
    public async Task<IActionResult> GetSocialStats()
    {
        try
        {
            var result = await _platformAnalyticsService.GetSocialStatsAsync();
            return Ok(new { message = "Platform social stats retrieved successfully.", data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve platform social stats.", details = ex.Message });
        }
    }
}

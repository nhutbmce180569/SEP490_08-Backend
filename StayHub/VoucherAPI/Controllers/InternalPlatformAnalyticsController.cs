using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

/// <summary>Internal platform voucher analytics for cross-service aggregation (Admin only).</summary>
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

    [HttpGet("vouchers")]
    public async Task<IActionResult> GetVoucherStats()
    {
        try
        {
            var result = await _platformAnalyticsService.GetVoucherStatsAsync();
            return Ok(new { message = M("PlatformVoucherStatsRetrievedSuccessfully"), data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("FailedToRetrievePlatformVoucherStats"), details = ex.Message });
        }
    }
}

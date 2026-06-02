using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

/// <summary>Internal platform voucher analytics for cross-service aggregation (Admin only).</summary>
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

    [HttpGet("vouchers")]
    public async Task<IActionResult> GetVoucherStats()
    {
        try
        {
            var result = await _platformAnalyticsService.GetVoucherStatsAsync();
            return Ok(new { message = "Platform voucher stats retrieved successfully.", data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve platform voucher stats.", details = ex.Message });
        }
    }
}

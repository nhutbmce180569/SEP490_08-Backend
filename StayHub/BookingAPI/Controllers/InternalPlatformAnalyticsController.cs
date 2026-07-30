using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace BookingAPI.Controllers
{
    /// <summary>Internal platform ops for health/overview (Admin only). Order analytics: customer-analytics.</summary>
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

        [HttpGet("operations")]
        public async Task<IActionResult> GetOperationsStats(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var result = await _platformAnalyticsService.GetOperationsStatsAsync(from, to);
                return Ok(new { message = M("PlatformOperationsStatsRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

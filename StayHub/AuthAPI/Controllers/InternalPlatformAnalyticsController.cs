using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace AuthAPI.Controllers
{
    /// <summary>Internal platform user analytics for cross-service aggregation (Admin only).</summary>
    [Route("api/internal/analytics/platform")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class InternalPlatformAnalyticsController : LocalizedControllerBase
    {
        private readonly IUserService _userService;

        public InternalPlatformAnalyticsController(IUserService userService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_userService = userService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUserStats(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string granularity = "day")
        {
            try
            {
                var result = await _userService.GetPlatformUserStatsAsync(from, to, granularity);
                return Ok(new { message = M("PlatformUserStatsRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("FailedToRetrievePlatformUserStats"), details = ex.Message });
            }
        }
    }
}

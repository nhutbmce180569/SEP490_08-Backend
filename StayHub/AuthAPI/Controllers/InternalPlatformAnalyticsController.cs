using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    /// <summary>Internal platform user analytics for cross-service aggregation (Admin only).</summary>
    [Route("api/internal/analytics/platform")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class InternalPlatformAnalyticsController : ControllerBase
    {
        private readonly IUserService _userService;

        public InternalPlatformAnalyticsController(IUserService userService)
        {
            _userService = userService;
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
                return Ok(new { message = "Platform user stats retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve platform user stats.", details = ex.Message });
            }
        }
    }
}

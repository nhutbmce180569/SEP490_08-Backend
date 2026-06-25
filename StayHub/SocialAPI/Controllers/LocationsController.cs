using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using SocialAPI.DTOs;
using SocialAPI.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SocialAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LocationsController : LocalizedControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationsController(ILocationService locationService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _locationService = locationService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("id")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID not found in token.");
        }

        [HttpPost("ping")]
        public async Task<IActionResult> PingLocation([FromBody] LocationPingDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _locationService.PingLocationAsync(userId, dto);
                return Ok(new { message = M("LocationPingedSuccessfully") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhilePingingLocation"), details = ex.Message });
            }
        }

        [HttpGet("friends/live")]
        public async Task<IActionResult> GetLiveFriendsLocations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var liveFriends = await _locationService.GetLiveFriendsLocationsAsync(userId);
                return Ok(new { message = M("LiveFriendsLocationsRetrievedSuccessfully"), data = liveFriends });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingFriendsLocations"), details = ex.Message });
            }
        }

        [HttpGet("schedules/{scheduleId}/live")]
        public async Task<IActionResult> GetLiveScheduleLocations(int scheduleId)
        {
            try
            {
                var liveLocations = await _locationService.GetLiveScheduleLocationsAsync(scheduleId);
                return Ok(new { message = M("LiveScheduleLocationsRetrievedSuccessfully"), data = liveLocations });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingScheduleLocations"), details = ex.Message });
            }
        }

        [HttpPost("share")]
        public async Task<IActionResult> GenerateTrackingToken()
        {
            try
            {
                var userId = GetCurrentUserId();
                var token = await _locationService.GenerateTrackingTokenAsync(userId);
                return Ok(new { message = M("TokenGeneratedSuccessfully"), data = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileGeneratingTrackingToken"), details = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("track/{token}")]
        public async Task<IActionResult> GetLocationByTrackingToken(string token)
        {
            try
            {
                var location = await _locationService.GetLocationByTrackingTokenAsync(token);
                return Ok(new { message = M("LocationRetrievedSuccessfully"), data = location });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingLocation"), details = ex.Message });
            }
        }

        // GET /api/locations/footprints
        // Tra ve toan bo dau chan (LocationLogs) cua nguoi dung -> "cao map" tu di chuyen.
        [HttpGet("footprints")]
        public async Task<IActionResult> GetMyFootprints()
        {
            try
            {
                var userId = GetCurrentUserId();
                var data = await _locationService.GetMyFootprintsAsync(userId);
                return Ok(new { message = M("FootprintsRetrievedSuccessfully"), data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingFootprints"), details = ex.Message });
            }
        }

        // GET /api/locations/heatmap?scheduleId=&days=90
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap([FromQuery] int? scheduleId, [FromQuery] int days = 90)
        {
            try
            {
                var data = await _locationService.GetHeatmapDataAsync(scheduleId, days);
                return Ok(new { message = M("HeatmapDataRetrievedSuccessfully"), data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingHeatmap"), details = ex.Message });
            }
        }
    }
}

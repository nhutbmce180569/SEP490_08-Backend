﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationsController(ILocationService locationService)
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
                return Ok(new { message = "Location pinged successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while pinging location.", details = ex.Message });
            }
        }

        [HttpGet("friends/live")]
        public async Task<IActionResult> GetLiveFriendsLocations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var liveFriends = await _locationService.GetLiveFriendsLocationsAsync(userId);
                return Ok(new { message = "Live friends locations retrieved successfully.", data = liveFriends });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving friends locations.", details = ex.Message });
            }
        }
    }
}

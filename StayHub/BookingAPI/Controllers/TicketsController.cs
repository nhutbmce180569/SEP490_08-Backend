using System.Security.Claims;
using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : LocalizedControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_ticketService = ticketService;
        }

        [HttpGet("my-tickets")]
        [Authorize]
        public async Task<IActionResult> GetMyTickets()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = M("UnableToDetermineCurrentUser") });
            }

            var tickets = await _ticketService.GetTicketsByUserIdAsync(userId.Value);
            return Ok(tickets);
        }

        [HttpGet("schedule/{scheduleId}")]
        [Authorize(Roles = "Staff,Manager,Admin")]
        public async Task<IActionResult> GetTicketsBySchedule(int scheduleId, [FromQuery] string? attendeeName = null, [FromQuery] string? checkInStatus = null)
        {
            var tickets = await _ticketService.GetTicketsByScheduleIdAsync(
                scheduleId, attendeeName, checkInStatus);
            return Ok(tickets);
        }


        [HttpPut("check-in")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDTO request)
        {
            try
            {
                var staffId = GetCurrentUserId();
                if (staffId == null)
                {
                    return Unauthorized(new { message = M("StaffIdentityNotDetermined") });
                }

                var result = await _ticketService.CheckInTicketAsync(request);

                return Ok(new
                {
                    message = M("CheckInSuccessful"),
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("CheckInSystemError"), error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}

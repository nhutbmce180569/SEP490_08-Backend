using System.Security.Claims;
using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet("my-tickets")]
        [Authorize]
        public async Task<IActionResult> GetMyTickets()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Unable to determine current user." });
            }

            var tickets = await _ticketService.GetTicketsByUserIdAsync(userId.Value);
            return Ok(tickets);
        }

        [HttpGet("schedule/{scheduleId}")]
        [Authorize]
        public async Task<IActionResult> GetTicketsBySchedule(int scheduleId)
        {
            var tickets = await _ticketService.GetTicketsByScheduleIdAsync(scheduleId);
            return Ok(tickets);
        }


        [HttpPut("check-in")]
        [Authorize(Roles = "Staff,Manager,Admin")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDTO request)
        {
            try
            {
                var staffId = GetCurrentUserId();
                if (staffId == null)
                {
                    return Unauthorized(new { message = "Không xác định được danh tính nhân viên." });
                }

                var result = await _ticketService.CheckInTicketAsync(request);

                return Ok(new
                {
                    message = "Điểm danh (Check-in) thành công!",
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
                return StatusCode(500, new { message = "Lỗi hệ thống trong quá trình điểm danh.", error = ex.Message });
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

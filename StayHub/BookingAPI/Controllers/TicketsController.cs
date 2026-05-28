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

        [HttpPut("check-in")]
        [Authorize]
        public async Task<IActionResult> CheckIn([FromBody] UpdateTicketDTO request)
        {
            var result = await _ticketService.CheckInTicketAsync(request);

            if (result == null)
            {
                return NotFound(new { message = "Ticket not found or invalid QR code." });
            }

            return Ok(new { message = "Ticket checked in successfully.", data = result });
        }
    }
}

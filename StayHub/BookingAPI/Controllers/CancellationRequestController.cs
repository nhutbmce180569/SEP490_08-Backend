using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CancellationRequestController : ControllerBase
    {
        private readonly ICancellationService _cancellationService;

        public CancellationRequestController(ICancellationService cancellationService)
        {
            _cancellationService = cancellationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCancellationRequest([FromBody] CreateCancellationRequestDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
                {
                    return Unauthorized(new { message = "User identity could not be verified." });
                }

                var result = await _cancellationService.CreateCancellationRequestAsync(customerId, dto);

                return Ok(new
                {
                    message = "Cancellation request submitted successfully. Please wait for admin approval.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAllRequests([FromQuery] string? status)
        {
            try
            {
                var result = await _cancellationService.GetCancellationRequestsAsync(status);
                return Ok(new
                {
                    message = "Retrieved cancellation requests successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            try
            {
                var result = await _cancellationService.GetCancellationRequestDetailsAsync(id);
                return Ok(new
                {
                    message = "Retrieved cancellation request details successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/process")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> ProcessRequest(int id, [FromBody] ProcessCancellationDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int processedByUserId))
                {
                    return Unauthorized(new { message = "Admin/Staff identity could not be verified." });
                }

                var result = await _cancellationService.ProcessCancellationRequestAsync(id, processedByUserId, dto);

                return Ok(new
                {
                    message = $"Cancellation request has been successfully {result.Status.ToLower()}.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
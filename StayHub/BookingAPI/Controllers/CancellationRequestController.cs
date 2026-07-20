using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CancellationRequestController : LocalizedControllerBase
    {
        private readonly ICancellationService _cancellationService;

        public CancellationRequestController(ICancellationService cancellationService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_cancellationService = cancellationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCancellationRequest([FromBody] CreateCancellationRequestDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
                {
                    return Unauthorized(new { message = M("UserIdentityCouldNotBeVerified") });
                }

                var result = await _cancellationService.CreateCancellationRequestAsync(customerId, dto);

                return Ok(new
                {
                    message = M("CancellationRequestSubmittedSuccessfullyPleaseWaitForAdmin"),
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
        public async Task<IActionResult> GetAllRequests(
            [FromQuery] string? status,
            [FromQuery] string? date,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 5;

                var operatorId = GetCurrentUserId();
                if (operatorId == null)
                    return Unauthorized(new { message = M("AdminStaffIdentityCouldNotBeVerified") });

                var result = await _cancellationService.GetCancellationRequestsAsync(operatorId.Value, status, date, page, pageSize);

                return Ok(new
                {
                    message = M("RetrievedCancellationRequestsSuccessfully"),
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
                var operatorId = GetCurrentUserId();
                if (operatorId == null)
                    return Unauthorized(new { message = M("AdminStaffIdentityCouldNotBeVerified") });

                var result = await _cancellationService.GetCancellationRequestDetailsAsync(id, operatorId.Value);
                return Ok(new
                {
                    message = M("RetrievedCancellationRequestDetailsSuccessfully"),
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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
                var operatorId = GetCurrentUserId();
                if (operatorId == null)
                    return Unauthorized(new { message = M("AdminStaffIdentityCouldNotBeVerified") });

                var result = await _cancellationService.ProcessCancellationRequestAsync(id, operatorId.Value, dto);

                return Ok(new
                {
                    message = $"Cancellation request has been successfully {result.Status.ToLower()}.",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}

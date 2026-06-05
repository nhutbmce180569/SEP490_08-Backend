using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using TourAPI.DTOs;
using TourAPI.Services;
using TourAPI.Services.Implements;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourSchedulesController : LocalizedControllerBase
    {
        private readonly ITourScheduleService _scheduleService;
        private readonly ITourScheduleStaffService _staffService;

        public TourSchedulesController(
            ITourScheduleService scheduleService,
            ITourScheduleStaffService staffService,
            IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _scheduleService = scheduleService;
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var result = await _scheduleService.GetAllSchedulesAsync(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTourScheduleDTO>> GetById(int id)
        {
            try
            {
                var result = await _scheduleService.GetScheduleByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("assigned")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAssignedSchedules()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                var assignedSchedules = await _staffService.GetAssignedSchedulesAsync(userId.Value);
                return Ok(assignedSchedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("SystemError"), details = ex.Message });
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

        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult<ReadTourScheduleDTO>> Create([FromBody] CreateTourScheduleDTO dto)
        {
            try
            {
                var result = await _scheduleService.CreateScheduleAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult<ReadTourScheduleDTO>> Update(int id, [FromBody] UpdateTourScheduleDTO dto)
        {
            try
            {
                var result = await _scheduleService.UpdateScheduleAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _scheduleService.DeleteScheduleAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/TourSchedules/{scheduleId}/itineraries
        [AllowAnonymous]
        [HttpGet("{scheduleId}/itineraries")]
        public async Task<IActionResult> GetScheduleItinerariesLocation(int scheduleId)
        {
            try
            {
                var result = await _scheduleService.GetItinerariesByScheduleIdAsync(scheduleId);
                return Ok(new { message = M("ItinerariesRetrievedSuccessfully"), data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/TourSchedules/batch
        [AllowAnonymous]
        [HttpPost("batch")]
        public async Task<ActionResult<IEnumerable<ReadTourScheduleDTO>>> GetBatchSchedules([FromBody] List<int> scheduleIds)
        {
            try
            {
                var result = await _scheduleService.GetSchedulesByIdsAsync(scheduleIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

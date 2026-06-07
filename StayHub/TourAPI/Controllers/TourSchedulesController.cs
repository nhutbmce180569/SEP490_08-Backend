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
        private readonly ITourAccessService _tourAccessService;

        public TourSchedulesController(
            ITourScheduleService scheduleService,
            ITourScheduleStaffService staffService,
            ITourAccessService tourAccessService,
            IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _scheduleService = scheduleService;
            _staffService = staffService;
            _tourAccessService = tourAccessService;
        }
        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? tourName = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var result = !string.IsNullOrWhiteSpace(tourName)
                    ? await _scheduleService.SearchSchedulesByTourNameAsync(tourName, page, pageSize)
                    : await _scheduleService.GetAllSchedulesAsync(page, pageSize);

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
                var userId = GetCurrentUserId();
                result.CanEdit = userId.HasValue &&
                    await _tourAccessService.CanEditAsync(
                        result.TourId,
                        userId.Value,
                        User.IsInRole("Admin"));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET: api/TourSchedules/my
        [HttpGet("my")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult> GetMySchedules(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });

                var result = await _scheduleService.GetSchedulesByCreatedByAsync(userId.Value, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult<ReadTourScheduleDTO>> Create([FromBody] CreateTourScheduleDTO dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized();
                if (!await _tourAccessService.CanEditAsync(dto.TourId, userId.Value, User.IsInRole("Admin")))
                    return Forbid();

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
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized();
                var existing = await _scheduleService.GetScheduleByIdAsync(id);
                if (!await _tourAccessService.CanEditAsync(existing.TourId, userId.Value, User.IsInRole("Admin")))
                    return Forbid();

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
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized();
                var existing = await _scheduleService.GetScheduleByIdAsync(id);
                if (!await _tourAccessService.CanEditAsync(existing.TourId, userId.Value, User.IsInRole("Admin")))
                    return Forbid();

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

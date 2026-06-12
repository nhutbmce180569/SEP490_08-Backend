using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using TourAPI.DTOs;
using TourAPI.Services;
using System.Security.Claims;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourScheduleStaffsController : LocalizedControllerBase
    {
        private readonly ITourScheduleStaffService _staffService;
        private readonly ITourAccessService _tourAccessService;

        public TourScheduleStaffsController(
            ITourScheduleStaffService staffService,
            ITourAccessService tourAccessService,
            IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _staffService = staffService;
            _tourAccessService = tourAccessService;
        }

        [HttpGet("assigned")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAssignedSchedules(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] bool upcomingOnly = false,
      [FromQuery] string? tourName = null) // ✅ thêm param
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });

                var result = await _staffService.GetAssignedSchedulesAsync(
                    userId.Value, page, pageSize, upcomingOnly, tourName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("SystemError"), details = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignStaff([FromBody] AssignStaffRequestDto dto)
        {
            try
            {
                if (dto == null || dto.ScheduleId <= 0 || dto.StaffId <= 0)
                    return BadRequest(new { message = M("ScheduleAndStaffIdMustBePositive") });

                var userId = GetCurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                if (!await _tourAccessService.CanEditScheduleAsync(
                        dto.ScheduleId,
                        userId.Value,
                        User.IsInRole("Admin")))
                    return Forbid();

                await _staffService.AssignStaffToScheduleAsync(dto);
                return Ok(new { message = M("StaffAssignedSuccessfully") });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("schedule/{scheduleId:int}/staff/{staffId:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RemoveStaff(int scheduleId, int staffId)
        {
            try
            {
                if (scheduleId <= 0 || staffId <= 0)
                    return BadRequest(new { message = M("ScheduleAndStaffIdMustBePositive") });

                var userId = GetCurrentUserId();
                if (!userId.HasValue) return Unauthorized();
                if (!await _tourAccessService.CanEditScheduleAsync(
                        scheduleId,
                        userId.Value,
                        User.IsInRole("Admin")))
                    return Forbid();

                await _staffService.RemoveStaffFromScheduleAsync(scheduleId, staffId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("schedule/{scheduleId:int}")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> GetStaffBySchedule(int scheduleId)
        {
            try
            {
                if (scheduleId <= 0)
                    return BadRequest(new { message = M("InvalidScheduleId") });

                var staffList = await _staffService.GetStaffByScheduleIdAsync(scheduleId);
                return Ok(new { message = M("StaffListRetrievedSuccessfully"), data = staffList });
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

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}

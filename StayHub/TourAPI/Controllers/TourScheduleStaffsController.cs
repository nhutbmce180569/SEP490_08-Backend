using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourScheduleStaffsController : LocalizedControllerBase
    {
        private readonly ITourScheduleStaffService _staffService;

        public TourScheduleStaffsController(ITourScheduleStaffService staffService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _staffService = staffService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignStaff([FromBody] AssignStaffRequestDto dto)
        {
            try
            {
                if (dto == null || dto.ScheduleId <= 0 || dto.StaffId <= 0)
                    return BadRequest(new { message = M("ScheduleAndStaffIdMustBePositive") });

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
    }
}

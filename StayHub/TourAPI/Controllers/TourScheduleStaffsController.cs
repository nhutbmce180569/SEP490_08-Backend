using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourScheduleStaffsController : ControllerBase
    {
        private readonly ITourScheduleStaffService _staffService;

        public TourScheduleStaffsController(ITourScheduleStaffService staffService)
        {
            _staffService = staffService;
        }

        /// <summary>
        /// Assign staff to a tour schedule
        /// </summary>
        /// <param name="dto">Assignment details including ScheduleId, StaffId, and AssignedRole</param>
        /// <returns>Status 200 if successful</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignStaff([FromBody] AssignStaffRequestDto dto)
        {
            try
            {
                if (dto == null || dto.ScheduleId <= 0 || dto.StaffId <= 0)
                {
                    return BadRequest(new { message = "ScheduleId và StaffId phải lớn hơn 0." });
                }

                await _staffService.AssignStaffToScheduleAsync(dto);
                return Ok(new { message = "Phân công nhân viên thành công." });
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

        /// <summary>
        /// Remove staff from a tour schedule
        /// </summary>
        /// <param name="scheduleId">Tour schedule ID</param>
        /// <param name="staffId">Staff ID</param>
        /// <returns>Status 204 No Content if successful</returns>
        [HttpDelete("schedule/{scheduleId:int}/staff/{staffId:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RemoveStaff(int scheduleId, int staffId)
        {
            try
            {
                if (scheduleId <= 0 || staffId <= 0)
                {
                    return BadRequest(new { message = "ScheduleId và StaffId phải lớn hơn 0." });
                }

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

        /// <summary>
        /// Get list of staff assigned to a specific tour schedule
        /// </summary>
        [HttpGet("schedule/{scheduleId:int}")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> GetStaffBySchedule(int scheduleId)
        {
            try
            {
                if (scheduleId <= 0) return BadRequest(new { message = "ScheduleId không hợp lệ." });

                var staffList = await _staffService.GetStaffByScheduleIdAsync(scheduleId);
                return Ok(new { message = "Lấy danh sách nhân viên thành công.", data = staffList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", details = ex.Message });
            }
        }
    }
}

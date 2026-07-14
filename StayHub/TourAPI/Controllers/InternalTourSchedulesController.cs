using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TourAPI.Models;

namespace TourAPI.Controllers
{
    [Route("api/internal/tourschedules")]
    [ApiController]
    public class InternalTourSchedulesController : ControllerBase
    {
        private readonly StayHubCatalogDbContext _context;
        private readonly IConfiguration _configuration;

        public InternalTourSchedulesController(StayHubCatalogDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private bool IsAuthorizedInternalRequest()
        {
            var internalKey = Request.Headers["X-Internal-Key"].ToString();
            var expectedKey = _configuration["InternalApi:SecretKey"];
            return !string.IsNullOrEmpty(internalKey) && internalKey == expectedKey;
        }

        // GET: api/internal/tourschedules/{scheduleId}/metadata
        [HttpGet("{scheduleId}/metadata")]
        public async Task<IActionResult> GetScheduleMetadata(int scheduleId)
        {
            if (!IsAuthorizedInternalRequest())
            {
                return Unauthorized(new { message = "Internal authorization required." });
            }

            var schedule = await _context.TourSchedules
                .Include(s => s.Tour)
                .Include(s => s.TourScheduleStaffs)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            return Ok(new {
                ScheduleId = schedule.Id,
                TourId = schedule.TourId,
                TourCreatedBy = schedule.Tour.CreatedBy,
                StaffIds = schedule.TourScheduleStaffs.Select(s => s.StaffId).ToList()
            });
        }

        // GET: api/internal/tourschedules/verify-manager?scheduleId=X&managerId=Y
        [HttpGet("verify-manager")]
        public async Task<IActionResult> VerifyManager([FromQuery] int scheduleId, [FromQuery] int managerId)
        {
            if (!IsAuthorizedInternalRequest())
            {
                return Unauthorized(new { message = "Internal authorization required." });
            }

            var isOwner = await _context.TourSchedules
                .Include(s => s.Tour)
                .AnyAsync(s => s.Id == scheduleId && s.Tour.CreatedBy == managerId);

            return Ok(new { isOwner = isOwner });
        }

        // GET: api/internal/tourschedules/{scheduleId}/staff-ids
        [HttpGet("{scheduleId}/staff-ids")]
        public async Task<IActionResult> GetStaffIds(int scheduleId)
        {
            if (!IsAuthorizedInternalRequest())
            {
                return Unauthorized(new { message = "Internal authorization required." });
            }

            var staffIds = await _context.TourScheduleStaffs
                .Where(s => s.ScheduleId == scheduleId)
                .Select(s => s.StaffId)
                .ToListAsync();

            return Ok(new { staffIds = staffIds });
        }

        // GET: api/internal/tourschedules/staff/{staffId}/schedule-ids
        [HttpGet("staff/{staffId}/schedule-ids")]
        public async Task<IActionResult> GetStaffScheduleIds(int staffId)
        {
            if (!IsAuthorizedInternalRequest())
            {
                return Unauthorized(new { message = "Internal authorization required." });
            }

            var scheduleIds = await _context.TourScheduleStaffs
                .Where(s => s.StaffId == staffId)
                .Select(s => s.ScheduleId)
                .ToListAsync();

            return Ok(new { scheduleIds = scheduleIds });
        }
    }
}

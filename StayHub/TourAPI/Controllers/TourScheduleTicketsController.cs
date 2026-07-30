using Microsoft.AspNetCore.Authorization;
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
    public class TourScheduleTicketsController : LocalizedControllerBase
    {
        private readonly ITourScheduleTicketService _service;
        private readonly ITourScheduleService _scheduleService;
        private readonly ITourAccessService _tourAccessService;

        public TourScheduleTicketsController(
            ITourScheduleTicketService service,
            ITourScheduleService scheduleService,
            ITourAccessService tourAccessService,
            IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _service = service;
            _scheduleService = scheduleService;
            _tourAccessService = tourAccessService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginationDTO<ReadTourScheduleTicketDTO>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAll(page, pageSize);
            return Ok(result);
        }

        [HttpGet("schedule/{scheduleId}")]
        public async Task<ActionResult<IEnumerable<ReadTourScheduleTicketDTO>>> GetBySchedule(int scheduleId)
        {
            try
            {
                var result = await _service.GetByScheduleId(scheduleId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> GetById(int id)
        {
            var result = await _service.GetById(id);
            if (result == null)
            {
                return NotFound(new { message = M("TourScheduleTicketNotFound") });
            }

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> Create([FromBody] CreateTourScheduleTicketDTO dto)
        {
            try
            {
                if (!await CanEditSchedule(dto.ScheduleId)) return Forbid();
                var result = await _service.Create(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> Update(int id, [FromBody] UpdateTourScheduleTicketDTO dto)
        {
            try
            {
                var existing = await _service.GetById(id);
                if (existing == null) return NotFound();
                if (!await CanEditSchedule(existing.ScheduleId)) return Forbid();
                var result = await _service.Update(id, dto);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message == "TourScheduleTicket not found")
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/change-status")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> ChangeStatus(int id)
        {
            try
            {
                var existing = await _service.GetById(id);
                if (existing == null) return NotFound();
                if (!await CanEditSchedule(existing.ScheduleId)) return Forbid();
                var result = await _service.ChangeStatus(id);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message == "TourScheduleTicket not found")
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/reserve")]
        public async Task<IActionResult> Reserve(int id, [FromBody] UpdateTourScheduleTicketQuantityDTO dto)
        {
            try
            {
                var reserved = await _service.Reserve(id, dto.Quantity);
                if (!reserved)
                {
                    return BadRequest(new { message = M("NotEnoughAvailableTicketsOrTicketTypeIsInactive") });
                }

                return Ok(new { message = M("TicketsReserved"), id, dto.Quantity });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/release")]
        public async Task<IActionResult> Release(int id, [FromBody] UpdateTourScheduleTicketQuantityDTO dto)
        {
            try
            {
                var released = await _service.Release(id, dto.Quantity);
                if (!released)
                {
                    return BadRequest(new { message = M("CouldNotReleaseTicketsForThisTicketType") });
                }

                return Ok(new { message = M("TicketsReleased"), id, dto.Quantity });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<bool> CanEditSchedule(int scheduleId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value
                               ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return false;

            var schedule = await _scheduleService.GetScheduleByIdAsync(scheduleId);
            return await _tourAccessService.CanEditAsync(
                schedule.TourId, userId, User.IsInRole("Admin"));
        }
    }
}

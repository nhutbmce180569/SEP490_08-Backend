using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourScheduleTicketsController : ControllerBase
    {
        private readonly ITourScheduleTicketService _service;

        public TourScheduleTicketsController(ITourScheduleTicketService service)
        {
            _service = service;
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
                return NotFound(new { message = "TourScheduleTicket not found" });
            }

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> Create([FromBody] CreateTourScheduleTicketDTO dto)
        {
            try
            {
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

        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> Activate(int id)
        {
            try
            {
                var result = await _service.Activate(id);
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

        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult<ReadTourScheduleTicketDTO>> Deactivate(int id)
        {
            try
            {
                var result = await _service.Deactivate(id);
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
                    return BadRequest(new { message = "Not enough available tickets or ticket type is inactive." });
                }

                return Ok(new { message = "Tickets reserved.", id, dto.Quantity });
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
                    return BadRequest(new { message = "Could not release tickets for this ticket type." });
                }

                return Ok(new { message = "Tickets released.", id, dto.Quantity });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

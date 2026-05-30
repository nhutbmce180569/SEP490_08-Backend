using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TourAPI.DTOs;
using TourAPI.Services;
using TourAPI.Services.Implements;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourSchedulesController : ControllerBase
    {
        private readonly ITourScheduleService _scheduleService;

        public TourSchedulesController(ITourScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        // Bất kỳ ai cũng có thể xem danh sách lịch trình
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadTourScheduleDTO>>> GetAll()
        {
            try
            {
                var result = await _scheduleService.GetAllSchedulesAsync();
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

        // Chỉ Admin/Operator được thao tác
        [HttpPost]
        [Authorize(Roles = "Operator,Admin")]
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
        [Authorize(Roles = "Operator,Admin")]
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
        [Authorize(Roles = "Operator,Admin")]
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
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TourScheduleItinerariesController : LocalizedControllerBase
    {
        private readonly ITourScheduleItineraryService _service;

        public TourScheduleItinerariesController(ITourScheduleItineraryService service, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_service = service;
        }

        [HttpGet("schedule/{scheduleId}")]
        public async Task<IActionResult> GetBySchedule(int scheduleId)
        {
            var result = await _service.GetByScheduleId(scheduleId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTourScheduleItineraryDTO>> Get(int id)
        {
            var result = await _service.GetById(id);
            if (result == null)
            {
                return NotFound(new { message = M("TourScheduleItineraryNotFound") });
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTourScheduleItineraryDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                var result = await _service.Add(dto, userId);
                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] CreateTourScheduleItineraryBatchDTO batch)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                await _service.AddBatch(batch, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTourScheduleItineraryDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                await _service.Update(id, dto, userId);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message == "TourScheduleItinerary not found")
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                await _service.Delete(id, userId);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message == "TourScheduleItinerary not found")
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

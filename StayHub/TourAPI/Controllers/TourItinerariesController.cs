using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TourItinerariesController : LocalizedControllerBase
    {
        private readonly ITourItineraryService _tourItineraryService;
        private readonly ITourAccessService _tourAccessService;

        public TourItinerariesController(ITourItineraryService tourItineraryService, ITourAccessService tourAccessService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_tourItineraryService = tourItineraryService;
            _tourAccessService = tourAccessService;
        }

        // GET: api/TourItineraries
        [HttpGet]
        public async Task<ActionResult> GetTourItineraries(int page = 1, int pageSize = 10)
        {
            var list = await _tourItineraryService.GetAll(page, pageSize);
            return Ok(list);
        }

        // GET: api/TourItineraries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTourItineraryDTO>> GetTourItinerary(int id)
        {
            var itinerary = await _tourItineraryService.GetById(id);

            if (itinerary == null)
            {
                return NotFound(new { message = M("TourItineraryNotFound") });
            }

            return itinerary;
        }

        // PUT: api/TourItineraries/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> PutTourItinerary(int id, [FromBody] UpdateTourItineraryDTO dto)
        {
            var existing = await _tourItineraryService.GetById(id);
            if (existing == null)
            {
                return NotFound(new { message = M("TourItineraryNotFound") });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
            }

            if (!await _tourAccessService.CanEditAsync(existing.TourId, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            try
            {
                await _tourItineraryService.Update(id, dto, userId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return NoContent();
        }

        // POST: api/TourItineraries
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult> PostTourItinerary([FromBody] CreateTourItineraryDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                if (!await _tourAccessService.CanEditAsync(dto.TourId, userId, User.IsInRole("Admin")))
                {
                    return Forbid();
                }

                await _tourItineraryService.Add(dto, userId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok();
        }

        // POST: api/TourItineraries/batch
        [HttpPost("batch")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult> PostTourItinerariesBatch([FromBody] CreateTourItineraryBatchDTO batch)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                var tourId = batch.Itineraries.FirstOrDefault()?.TourId;
                if (!tourId.HasValue ||
                    !await _tourAccessService.CanEditAsync(tourId.Value, userId, User.IsInRole("Admin")))
                {
                    return Forbid();
                }

                await _tourItineraryService.AddBatch(batch, userId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok();
        }

        // DELETE: api/TourItineraries/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> DeleteTourItinerary(int id)
        {
            var existing = await _tourItineraryService.GetById(id);
            if (existing == null)
            {
                return NotFound(new { message = M("TourItineraryNotFound") });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
            }

            if (!await _tourAccessService.CanEditAsync(existing.TourId, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            try
            {
                await _tourItineraryService.Delete(id);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return NoContent();
        }

        [HttpGet("import-template")]
        [Authorize(Roles = "Manager,Admin")]
        public IActionResult DownloadImportTemplate()
        {
            var content = _tourItineraryService.CreateImportTemplate();
            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StayHub_Tour_Itinerary_Template.xlsx");
        }

    }
}

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : LocalizedControllerBase
    {
        private readonly ITourService _tourService;
        private readonly ITourAccessService _tourAccessService;
        private readonly IMapper _mapper;

        public ToursController(ITourService tourService, ITourAccessService tourAccessService, IMapper mapper, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _tourService = tourService;
            _tourAccessService = tourAccessService;
            _mapper = mapper;
        }

        // GET: api/Tours/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAllToursForAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, [FromQuery] int? managerId = null)
        {
            try
            {
                var list = await _tourService.GetByAdmin(page, pageSize, searchTerm, managerId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Tours/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTourStatus(int id, [FromBody] UpdateTourStatusRequest request)
        {
            try
            {
                // Gọi hàm UpdateTourStatusAsync mà chúng ta đã viết ở TourService
                await _tourService.UpdateTourStatusAsync(id, request.Status);

                return Ok(new { message = $"Tour status successfully updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Tours/{id}/manager
        [HttpPut("{id}/manager")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeTourManager(int id, [FromBody] ChangeTourManagerRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }
                var existingTour = await _tourService.GetById(id);
                if (existingTour == null)
                {
                    return NotFound(new { message = M("TourNotFound") });
                }

                await _tourService.ChangeManagerAsync(id, request.ManagerId, userId.Value);

                return Ok(new { message = "Tour manager successfully changed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        // GET: api/Tours
        [HttpGet]
        public async Task<ActionResult> GetTours(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool createdByMe = false)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
            }

            var list = await _tourService.GetAll(
                page, pageSize, userId.Value, User.IsInRole("Admin"), searchTerm, categoryId, createdByMe);
            return Ok(list);
        }



        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult> SearchTours(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? country = null,
            [FromQuery] string? city = null,
            [FromQuery] long? minPrice = null,
            [FromQuery] long? maxPrice = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? duration = null,
            [FromQuery] string? sortBy = null)
        {
            var list = await _tourService.SearchTours(page, pageSize, searchTerm, categoryId, country, city, minPrice, maxPrice, startDate, endDate, duration, sortBy);
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("public/region/{region}")]
        public async Task<ActionResult> GetToursByRegion(string region, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var list = await _tourService.GetToursByRegion(region, page, pageSize);
            return Ok(list);
        }


        // GET: api/Tours
        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<ActionResult> GetPublicTours(int page = 1, int pageSize = 10)
        {
            var list = await _tourService.GetActiveTours(page, pageSize);
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("sale")]
        public async Task<ActionResult> GetSaleTours([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var list = await _tourService.GetSaleTours(page, pageSize);
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("hot")]
        public async Task<ActionResult> GetHotTours([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var list = await _tourService.GetHotTours(page, pageSize);
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("upcoming")]
        public async Task<ActionResult> GetUpcomingTours([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var list = await _tourService.GetUpcomingTours(page, pageSize);
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("public/{id}")]
        public async Task<ActionResult<ReadTourDTO>> GetPublicTour(int id)
        {
            try
            {
                var tour = await _tourService.GetActiveTour(id);

                if (tour == null)
                {
                    return NotFound();
                }
                return tour;

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/Tours/active/5
        [HttpPut("active/{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult> Active(int id, bool isActive)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
            }

            if (!await _tourAccessService.CanEditAsync(id, userId.Value, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            await _tourService.ActiveTour(id, isActive);

            return Ok();
        }

        // GET: api/Tours/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTourDTO>> GetTour(int id)
        {
            var userId = GetCurrentUserId();
            var tour = await _tourService.GetById(id, userId, User.IsInRole("Admin"));

            if (tour == null)
            {
                return NotFound();
            }

            return tour;
        }

        // GET: api/Tours/manager
        [HttpGet("manager")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult> GetToursByManager([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                var list = await _tourService.GetByManager(userId.Value, page, pageSize, searchTerm);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Tours/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> PutTour(int id, [FromForm] UpdateTourDTO tourDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
            }

            var existingTour = await _tourService.GetById(id);
            if (existingTour == null)
            {
                return NotFound(new { message = M("TourNotFound") });
            }

            if (!await _tourAccessService.CanEditAsync(id, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            try
            {
                await _tourService.Update(id, tourDto, userId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return NoContent();
        }

        // POST: api/Tours
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult<ReadTourDTO>> PostTour([FromForm] CreateTourDTO tourDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
                }

                await _tourService.Add(tourDto, userId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok();
        }

        // DELETE: api/Tours/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
            }

            var tour = await _tourService.GetById(id);
            if (tour == null)
            {
                return NotFound(new { message = M("TourNotFound") });
            }

            if (!await _tourAccessService.CanEditAsync(id, userId, User.IsInRole("Admin")))
            {
                return Forbid();
            }

            try
            {
                await _tourService.Delete(id);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return NoContent();
        }

        [HttpGet("category/{categoryId}/count")]
        [AllowAnonymous]
        public async Task<ActionResult<int>> CountToursByCategory(int categoryId)
        {
            var count = await _tourService.CountToursByCategoryIdAsync(categoryId);
            return Ok(count);
        }

        // GET: api/Tours/{tourId}/itineraries
        [AllowAnonymous]
        [HttpGet("{tourId}/itineraries")]
        public async Task<IActionResult> GetTourItinerariesLocation(int tourId)
        {
            try
            {
                var result = await _tourService.GetItinerariesByTourIdAsync(tourId);
                return Ok(new { message = M("ItinerariesRetrievedSuccessfully"), data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("batch")]
        public async Task<ActionResult<IEnumerable<ReadTourDTO>>> GetBatchTours([FromBody] List<int> tourIds)
        {
            try
            {
                if (tourIds == null || !tourIds.Any()) return BadRequest("Danh sách ID không được rỗng.");

                var result = await _tourService.GetToursByIdsAsync(tourIds);
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
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        [AllowAnonymous]
        [HttpPost("request-consultation")]
        public async Task<IActionResult> RequestConsultation([FromBody] ConsultationRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _tourService.RequestConsultationAsync(request);
                return Ok(new { message = "Your consultation request has been sent successfully. We will contact you soon." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your consultation request.", error = ex.Message });
            }
        }
    }
}

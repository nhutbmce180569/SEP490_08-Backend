using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class ToursController : ControllerBase
    {
        private readonly ITourService _tourService;
        private readonly IMapper _mapper;

        public ToursController(ITourService tourService, IMapper mapper)
        {
            _tourService = tourService;
            _mapper = mapper;
        }

        // GET: api/Tours/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> GetAllToursForAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            try
            {
                var list = await _tourService.GetByAdmin(page, pageSize, searchTerm);
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


        // GET: api/Tours
        [HttpGet]
        public async Task<ActionResult> GetTours(int page = 1, int pageSize = 10)
        {
            var list = await _tourService.GetAll(page, pageSize);
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


        // GET: api/Tours
        [HttpGet("public")]
        public async Task<ActionResult> GetPublicTours(int page = 1, int pageSize = 10)
        {
            var list = await _tourService.GetActiveTours(page, pageSize);
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
        public async Task<ActionResult> Active(int id, bool isActive)
        {
            await _tourService.ActiveTour(id, isActive);

            return Ok();
        }

        // GET: api/Tours/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReadTourDTO>> GetTour(int id)
        {
            var tour = await _tourService.GetById(id);

            if (tour == null)
            {
                return NotFound();
            }

            return tour;
        }

        // PUT: api/Tours/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTour(int id, [FromForm] UpdateTourDTO tourDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Cannot extract user ID from token" });
            }

            var existingTour = await _tourService.GetById(id);
            if (existingTour == null)
            {
                return NotFound(new { message = "Tour not found" });
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
        public async Task<ActionResult<ReadTourDTO>> PostTour([FromForm] CreateTourDTO tourDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Cannot extract user ID from token" });
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
        public async Task<IActionResult> DeleteTour(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Cannot extract user ID from token" });
            }

            var tour = await _tourService.GetById(id);
            if (tour == null)
            {
                return NotFound(new { message = "Tour not found" });
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
    }
}

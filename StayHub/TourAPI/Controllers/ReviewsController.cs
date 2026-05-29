using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new Exception("Cannot extract user ID from token.");
            }

            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDTO request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _reviewService.CreateReviewAsync(request, currentUserId);
                return CreatedAtAction(nameof(GetMyReviewByTour), new { tourId = result.TourId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDTO request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _reviewService.UpdateReviewAsync(id, request, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _reviewService.GetMyReviewsAsync(currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("tour/{tourId}/mine")]
        public async Task<IActionResult> GetMyReviewByTour(int tourId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var review = await _reviewService.GetMyReviewByTourAsync(tourId, currentUserId);
                if (review == null)
                    return NotFound(new { message = "No review found for this tour." });

                return Ok(review);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("tour/{tourId}")]
        public async Task<IActionResult> GetReviewsByTour(int tourId)
        {
            try
            {
                var result = await _reviewService.GetReviewsByTourAsync(tourId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System;
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
    public class ReviewsController : LocalizedControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(
            IReviewService reviewService,
            IStringLocalizer<Messages> localizer)
            : base(localizer)
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
                    return NotFound(new { message = M("NoReviewFoundForThisTour") });

                return Ok(review);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("tour/{tourId}")]
        public async Task<IActionResult> GetReviewsByTour(
              int tourId,
              [FromQuery] int page = 1,
              [FromQuery] int pageSize = 5,
              [FromQuery] int? rating = null,
              [FromQuery] string sortOrder = "newest")
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 5;

                var result = await _reviewService.GetReviewsByTourAsync(tourId, page, pageSize, rating, sortOrder, includeHidden: false);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Manager,Staff,Admin")]
        [HttpGet("tour/{tourId}/admin")]
        public async Task<IActionResult> GetReviewsByTourAdmin(
            int tourId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] int? rating = null,
            [FromQuery] string sortOrder = "newest")
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 5;

                var currentUserId = GetCurrentUserId();

                var result = await _reviewService.GetReviewsByTourAsync(tourId, page, pageSize, rating, sortOrder, includeHidden: true);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/reviews/manager/tour/{tourId}
        [Authorize(Roles = "Manager")]
        [HttpGet("manager/tour/{tourId}")]
        public async Task<IActionResult> GetReviewsByManager(
            int tourId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] int? rating = null,
            [FromQuery] string sortOrder = "newest")
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 5;

                var currentUserId = GetCurrentUserId();

                var result = await _reviewService.GetReviewsByManagerAsync(
                    currentUserId, tourId, page, pageSize, rating, sortOrder);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Manager,Staff,Admin")]
        [HttpPost("{reviewId}/replies")]
        public async Task<IActionResult> CreateReviewReply(int reviewId, [FromBody] CreateReviewReplyDTO request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                request.ReviewId = reviewId;
                var result = await _reviewService.CreateReviewReplyAsync(currentUserId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Manager,Staff,Admin")]
        [HttpPut("replies/{replyId}")]
        public async Task<IActionResult> UpdateReviewReply(int replyId, [FromBody] UpdateReviewReplyDTO request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _reviewService.UpdateReviewReplyAsync(replyId, currentUserId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Manager,Staff,Admin")]
        [HttpDelete("replies/{replyId}")]
        public async Task<IActionResult> DeleteReviewReply(int replyId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _reviewService.DeleteReviewReplyAsync(replyId, currentUserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Manager,Staff,Admin")]
        [HttpPatch("{id}/hide")]
        public async Task<IActionResult> HideReview(int id, [FromQuery] bool hidden = true)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _reviewService.HideReviewAsync(id, hidden);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

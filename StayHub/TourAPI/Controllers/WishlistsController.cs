using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistsController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistsController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
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

        [HttpGet]
        public async Task<IActionResult> GetMyWishlist()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _wishlistService.GetMyWishlistAsync(currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("tours/{tourId}")]
        public async Task<IActionResult> AddTourToWishlist(int tourId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _wishlistService.AddToWishlistAsync(tourId, currentUserId);
                return CreatedAtAction(nameof(GetMyWishlist), result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("tours/{tourId}")]
        public async Task<IActionResult> RemoveTourFromWishlist(int tourId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _wishlistService.RemoveFromWishlistAsync(tourId, currentUserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

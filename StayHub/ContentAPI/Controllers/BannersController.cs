using ContentAPI.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly IBannerService _bannerService;

        public BannersController(IBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        // GET: api/Banners?page=1&pageSize=10
        [HttpGet]
        [AllowAnonymous] // Everyone can see banners
        public async Task<ActionResult<PaginationDTO<ReadBannerDTO>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var banners = await _bannerService.GetAllBanners(page, pageSize);
            return Ok(banners);
        }

        // GET: api/Banners/search?keyword=summer&page=1&pageSize=10
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery(Name = "q")] string keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(new
                    {
                        message = "Search keyword cannot be empty.",
                        data = new PaginationDTO<ReadBannerDTO>
                        {
                            Data = new List<ReadBannerDTO>(),
                            Total = 0,
                            TotalPages = 0,
                            CurrentPage = page,
                            PageSize = pageSize
                        }
                    });
                }

                var result = await _bannerService.SearchBannersAsync(keyword, page, pageSize);

                if (result.Total == 0)
                {
                    return Ok(new
                    {
                        message = $"No banners found matching '{keyword}'.",
                        data = result
                    });
                }

                return Ok(new
                {
                    message = $"Found {result.Total} banner(s) matching '{keyword}'.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching for banners.", details = ex.Message });
            }
        }

        // GET: api/Banners/active?page=1&pageSize=10
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationDTO<ReadBannerDTO>>> GetActive([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var banners = await _bannerService.GetActiveBanners(page, pageSize);
            return Ok(banners);
        }

        // GET: api/Banners/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReadBannerDTO>> GetById(int id)
        {
            var banner = await _bannerService.GetBannerById(id);
            if (banner == null)
            {
                return NotFound(new { message = $"Banner with ID {id} not found." });
            }
            return Ok(banner);
        }

        // POST: api/Banners
        [HttpPost]
        [Authorize(Roles = "Admin")] // Only Admin can create banners
        [Consumes("multipart/form-data")] // Required for file upload
        public async Task<ActionResult<ReadBannerDTO>> Create([FromForm] CreateBannerDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdBanner = await _bannerService.CreateBanner(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdBanner.Id }, createdBanner);
        }

        // PUT: api/Banners/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateBannerDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _bannerService.UpdateBanner(id, dto);
            if (!result)
            {
                return NotFound(new { message = $"Cannot update. Banner with ID {id} not found." });
            }

            return Ok(new { message = "Banner updated successfully." });
        }

        // DELETE: api/Banners/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _bannerService.DeleteBanner(id);
            if (!result)
            {
                return NotFound(new { message = $"Cannot delete. Banner with ID {id} not found." });
            }

            return Ok(new { message = "Banner deleted successfully." });
        }

        // PATCH: api/Banners/5/activate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _bannerService.ChangeBannerStatus(id, true);
            if (!result)
            {
                return NotFound(new { message = $"Cannot activate. Banner with ID {id} not found." });
            }

            return Ok(new { message = "Banner activated successfully." });
        }

        // PATCH: api/Banners/5/deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _bannerService.ChangeBannerStatus(id, false);
            if (!result)
            {
                return NotFound(new { message = $"Cannot deactivate. Banner with ID {id} not found." });
            }

            return Ok(new { message = "Banner deactivated successfully." });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    /// <summary>
    /// Admin platform analytics (ecosystem, catalog, social, voucher, health).
    /// Customer/order/review behavior: use <c>/api/customer-analytics</c> instead.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/platform-analytics")]
    [ApiController]
    public class PlatformAnalyticsController : ControllerBase
    {
        private readonly IPlatformAnalyticsService _platformAnalyticsService;

        public PlatformAnalyticsController(IPlatformAnalyticsService platformAnalyticsService)
        {
            _platformAnalyticsService = platformAnalyticsService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] PlatformAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _platformAnalyticsService.GetOverviewAsync(query.From, query.To);
                return Ok(new { message = "Platform analytics overview retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] PlatformAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _platformAnalyticsService.GetUsersAsync(query.From, query.To);
                return Ok(new { message = "Platform user analytics retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog([FromQuery] PlatformAnalyticsQueryDTO query)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid query parameters.", errors = ModelState });
                }

                var result = await _platformAnalyticsService.GetCatalogAsync(query.Top);
                return Ok(new { message = "Platform catalog analytics retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("vouchers")]
        public async Task<IActionResult> GetVouchers()
        {
            try
            {
                var result = await _platformAnalyticsService.GetVouchersAsync();
                return Ok(new { message = "Platform voucher analytics retrieved successfully.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("social")]
        public async Task<IActionResult> GetSocial()
        {
            try
            {
                var result = await _platformAnalyticsService.GetSocialAsync();
                return Ok(new { message = "Platform social analytics retrieved successfully.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth([FromQuery] PlatformAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _platformAnalyticsService.GetHealthAsync(query.From, query.To);
                return Ok(new { message = "Platform health analytics retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

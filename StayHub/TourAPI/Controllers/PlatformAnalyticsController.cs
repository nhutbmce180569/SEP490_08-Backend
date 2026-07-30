using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
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
    public class PlatformAnalyticsController : LocalizedControllerBase
    {
        private readonly IPlatformAnalyticsService _platformAnalyticsService;

        public PlatformAnalyticsController(IPlatformAnalyticsService platformAnalyticsService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_platformAnalyticsService = platformAnalyticsService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] PlatformAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _platformAnalyticsService.GetOverviewAsync(query.From, query.To);
                return Ok(new { message = M("PlatformAnalyticsOverviewRetrievedSuccessfully"), data = result });
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
                return Ok(new { message = M("PlatformUserAnalyticsRetrievedSuccessfully"), data = result });
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
                    return BadRequest(new { message = M("InvalidQueryParameters"), errors = ModelState });
                }

                var result = await _platformAnalyticsService.GetCatalogAsync(query.Top);
                return Ok(new { message = M("PlatformCatalogAnalyticsRetrievedSuccessfully"), data = result });
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
                return Ok(new { message = M("PlatformVoucherAnalyticsRetrievedSuccessfully"), data = result });
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
                return Ok(new { message = M("PlatformSocialAnalyticsRetrievedSuccessfully"), data = result });
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
                return Ok(new { message = M("PlatformHealthAnalyticsRetrievedSuccessfully"), data = result });
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Controllers
{
    /// <summary>Manager dashboard APIs for customer analytics (overview, segments, trends, engagement).</summary>
    [Authorize(Roles = "Manager,Admin")]
    [Route("api/customer-analytics")]
    [ApiController]
    public class CustomerAnalyticsController : LocalizedControllerBase
    {
        private readonly ICustomerAnalyticsService _customerAnalyticsService;

        public CustomerAnalyticsController(ICustomerAnalyticsService customerAnalyticsService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_customerAnalyticsService = customerAnalyticsService;
        }

        /// <summary>Returns KPI summary for the selected date range (defaults to last 30 days).</summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] CustomerAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _customerAnalyticsService.GetOverviewAsync(query.From, query.To);
                return Ok(new { message = M("CustomerAnalyticsOverviewRetrievedSuccessfully"), data = result });
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

        /// <summary>Returns customer demographics breakdown (gender, age, provider, status).</summary>
        [HttpGet("demographics")]
        public async Task<IActionResult> GetDemographics([FromQuery] CustomerAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _customerAnalyticsService.GetDemographicsAsync(query.From, query.To);
                return Ok(new { message = M("CustomerDemographicsRetrievedSuccessfully"), data = result });
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

        /// <summary>Returns customer segments (never purchased, one-time, repeat, at-risk, high-value).</summary>
        [HttpGet("segments")]
        public async Task<IActionResult> GetSegments([FromQuery] CustomerAnalyticsQueryDTO query)
        {
            try
            {
                var result = await _customerAnalyticsService.GetSegmentsAsync(query.From, query.To);
                return Ok(new { message = M("CustomerSegmentsRetrievedSuccessfully"), data = result });
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

        /// <summary>Returns registration, order, and revenue trends over time.</summary>
        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends([FromQuery] CustomerAnalyticsQueryDTO query)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = M("InvalidQueryParameters"), errors = ModelState });
                }

                var result = await _customerAnalyticsService.GetTrendsAsync(
                    query.From, query.To, query.Granularity);

                return Ok(new { message = M("CustomerTrendsRetrievedSuccessfully"), data = result });
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

        /// <summary>Returns top customers by total spend in the selected period.</summary>
        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers([FromQuery] CustomerAnalyticsQueryDTO query)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = M("InvalidQueryParameters"), errors = ModelState });
                }

                var result = await _customerAnalyticsService.GetTopCustomersAsync(
                    query.Top, query.From, query.To);

                return Ok(new { message = M("TopCustomersRetrievedSuccessfully"), data = result });
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

        /// <summary>Returns review and wishlist engagement metrics.</summary>
        [HttpGet("engagement")]
        public async Task<IActionResult> GetEngagement()
        {
            try
            {
                var result = await _customerAnalyticsService.GetEngagementAsync();
                return Ok(new { message = M("CustomerEngagementAnalyticsRetrievedSuccessfully"), data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Returns a paginated customer list with per-customer analytics metrics.</summary>
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers([FromQuery] CustomerListQueryDTO query)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = M("InvalidQueryParameters"), errors = ModelState });
                }

                var result = await _customerAnalyticsService.GetCustomerListAsync(query);
                return Ok(new { message = M("CustomerAnalyticsListRetrievedSuccessfully"), data = result });
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

        /// <summary>Returns detailed analytics for a single customer.</summary>
        [HttpGet("customers/{customerId}")]
        public async Task<IActionResult> GetCustomerDetail(
            int customerId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var result = await _customerAnalyticsService.GetCustomerDetailAsync(customerId, from, to);
                if (result == null)
                {
                    return NotFound(new { message = M("CustomerNotFound") });
                }

                return Ok(new { message = M("CustomerDetailAnalyticsRetrievedSuccessfully"), data = result });
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

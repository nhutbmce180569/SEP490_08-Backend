using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace BookingAPI.Controllers
{
    /// <summary>Internal order analytics endpoints for cross-service aggregation (Manager/Admin only).</summary>
    [Route("api/internal/analytics/orders")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")]
    public class InternalOrderAnalyticsController : LocalizedControllerBase
    {
        private readonly IOrderAnalyticsService _orderAnalyticsService;

        public InternalOrderAnalyticsController(IOrderAnalyticsService orderAnalyticsService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_orderAnalyticsService = orderAnalyticsService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var result = await _orderAnalyticsService.GetOverviewAsync(from, to);
                return Ok(new { message = M("OrderOverviewRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("segments")]
        public async Task<IActionResult> GetSegments(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int totalCustomers = 0)
        {
            try
            {
                var result = await _orderAnalyticsService.GetSegmentsAsync(from, to, totalCustomers);
                return Ok(new { message = M("CustomerOrderSegmentsRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("trends")]
        public async Task<IActionResult> GetTrends(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string granularity = "day")
        {
            try
            {
                var result = await _orderAnalyticsService.GetTrendsAsync(from, to, granularity);
                return Ok(new { message = M("OrderTrendsRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("booking-statistics")]
        public async Task<IActionResult> GetBookingStatistics([FromBody] BookingStatisticsRequestDTO request)
        {
            try
            {
                var result = await _orderAnalyticsService.GetBookingStatisticsAsync(request);
                return Ok(new { message = "Booking statistics retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers(
            [FromQuery] int top = 10,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var result = await _orderAnalyticsService.GetTopCustomersAsync(top, from, to);
                return Ok(new { message = M("TopCustomersRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("customer-metrics")]
        public async Task<IActionResult> GetCustomerMetrics(
            [FromBody] CustomerMetricsRequest request,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                if (request?.CustomerIds == null || request.CustomerIds.Count == 0)
                {
                    return BadRequest(new { message = M("CustomerIDsListCannotBeEmpty") });
                }

                if (request.CustomerIds.Count > 100)
                {
                    return BadRequest(new { message = M("CannotRequestMetricsForMoreThan100CustomersAtOnce") });
                }

                var result = await _orderAnalyticsService.GetCustomerMetricsAsync(request.CustomerIds, from, to);
                return Ok(new { message = M("CustomerOrderMetricsRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("customer-metrics/{customerId}")]
        public async Task<IActionResult> GetCustomerMetricsById(
            int customerId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var result = await _orderAnalyticsService.GetCustomerMetricsByIdAsync(customerId, from, to);
                if (result == null)
                {
                    return Ok(new { message = M("NoOrderDataFoundForThisCustomer"), data = (object?)null });
                }

                return Ok(new { message = M("CustomerOrderMetricsRetrievedSuccessfully"), data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CustomerMetricsRequest
    {
        public List<int> CustomerIds { get; set; } = [];
    }
}

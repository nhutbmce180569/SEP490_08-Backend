using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingAPI.Controllers
{
    /// <summary>Internal order analytics endpoints for cross-service aggregation (Manager/Admin only).</summary>
    [Route("api/internal/analytics/orders")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")]
    public class InternalOrderAnalyticsController : ControllerBase
    {
        private readonly IOrderAnalyticsService _orderAnalyticsService;

        public InternalOrderAnalyticsController(IOrderAnalyticsService orderAnalyticsService)
        {
            _orderAnalyticsService = orderAnalyticsService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var result = await _orderAnalyticsService.GetOverviewAsync(from, to);
                return Ok(new { message = "Order overview retrieved successfully.", data = result });
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
                return Ok(new { message = "Customer order segments retrieved successfully.", data = result });
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
                return Ok(new { message = "Order trends retrieved successfully.", data = result });
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
                return Ok(new { message = "Top customers retrieved successfully.", data = result });
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
                    return BadRequest(new { message = "Customer IDs list cannot be empty." });
                }

                if (request.CustomerIds.Count > 100)
                {
                    return BadRequest(new { message = "Cannot request metrics for more than 100 customers at once." });
                }

                var result = await _orderAnalyticsService.GetCustomerMetricsAsync(request.CustomerIds, from, to);
                return Ok(new { message = "Customer order metrics retrieved successfully.", data = result });
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
                    return Ok(new { message = "No order data found for this customer.", data = (object?)null });
                }

                return Ok(new { message = "Customer order metrics retrieved successfully.", data = result });
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

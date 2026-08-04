using System.Security.Claims;
using BookingAPI.DTOs;
using BookingAPI.Exceptions;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System.Security.Cryptography;
using System.Text;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : LocalizedControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IIdempotencyService _idempotencyService;

        public OrdersController(IOrderService orderService, IIdempotencyService idempotencyService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _orderService = orderService;
            _idempotencyService = idempotencyService;
        }

        // POST: api/orders/check-completed-booking
        [HttpPost("check-completed-booking")]
        public async Task<IActionResult> CheckCompletedBooking([FromBody] CheckBookingRequest request)
        {
            var hasBooked = await _orderService.CheckCompletedBookingAsync(request);

            return Ok(hasBooked); 
        }
        // POST: api/orders/check-booking
        [HttpPost("check-booking")]
        [Authorize]
        public async Task<IActionResult> CheckBooking([FromBody] CheckBookingTour request)
        {
            var hasBooked = await _orderService.CheckBookingAsync(request);

            return Ok(hasBooked);
        }

        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO request)
        {
            // Lấy CustomerId từ Token đăng nhập của User
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Unauthorized(new { message = M("InvalidTokenClaimsUserNotIdentified") });
            }

            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return BadRequest(new { message = "Idempotency-Key header is required." });
            }

            var existingState = await _idempotencyService.GetStateAsync(idempotencyKey, customerId);
            if (existingState != null)
            {
                if (existingState.Status == "Processing")
                {
                    return Conflict(new { message = "This order request is already being processed." });
                }
                
                if (existingState.Status == "Completed" && existingState.OrderId.HasValue)
                {
                    var existingOrder = await _orderService.GetOrderByIdAsync(existingState.OrderId.Value, customerId);
                    if (existingOrder != null)
                    {
                        return StatusCode(201, new { message = M("OrderAndTicketsCreatedSuccessfully"), data = existingOrder });
                    }
                }
            }

            var lockAcquired = await _idempotencyService.TryAcquireLockAsync(idempotencyKey, customerId);
            if (!lockAcquired)
            {
                return Conflict(new { message = "This order request is already being processed." });
            }

            try
            {
                var result = await _orderService.CreateOrderAsync(customerId, request, idempotencyKey);
                return StatusCode(201, new { message = M("OrderAndTicketsCreatedSuccessfully"), data = result });
            }
            catch (BookingValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my/{id}")]
        [Authorize]
        public async Task<IActionResult> GetMyOrderById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Unauthorized(new { message = M("InvalidTokenClaimsUserNotIdentified") });
            }

            var result = await _orderService.GetOrderByIdAsync(id, customerId);
            
            if (result == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }
            
            return Ok(new { message = M("OrderRetrievedSuccessfully"), data = result });
        }

        [HttpGet("schedule/{scheduleId}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByScheduleId(int scheduleId)
        {
            var result = await _orderService.GetOrdersByScheduleIdAsync(scheduleId);

            if (result == null || !result.Any())
            {
                return Ok(new { message = $"No orders found for Schedule ID {scheduleId}.", data = new List<ReadOrderDTO>() });
            }

            return Ok(new { message = M("OrdersRetrievedSuccessfully"), data = result });
        }

        [HttpGet("schedule/{scheduleId}/customers")]
        [Authorize]
        public async Task<IActionResult> GetCustomersByScheduleId(int scheduleId, [FromQuery] string? attendeeName = null) 
        {
            var result = await _orderService.GetScheduleCustomersAsync(scheduleId, attendeeName);

            if (result == null || !result.Any())
                return Ok(new { message = $"No customers found for Schedule ID {scheduleId}.", data = new List<ScheduleCustomerDTO>() });

            return Ok(new { message = M("ScheduleCustomersRetrievedSuccessfully"), data = result });
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByUserId(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            // Lấy CustomerId từ Token đăng nhập của User
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int customerId))
            {
                return Unauthorized(new { message = M("InvalidTokenClaimsUserNotIdentified") });
            }

            if (customerId != userId)
            {
                return Forbid();
            }

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var result = await _orderService.GetOrdersByUserIdAsync(userId, page, pageSize, status);

            if (result == null || result.Data == null || !result.Data.Any())
            {
                return Ok(new { message = $"No orders found for User ID {userId}.", data = new PaginationDTO<ReadOrderDTO> { Data = new List<ReadOrderDTO>(), Total = 0, TotalPages = 0, CurrentPage = page, PageSize = pageSize } });
            }

            return Ok(new { message = M("OrdersRetrievedSuccessfully"), data = result });
        }

        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var cancelled = await _orderService.CancelOrderAsync(id);
            if (!cancelled)
            {
                return BadRequest(new { message = M("OrderCannotBeCancelledItMayNotExistOrIsNoLongerPending") });
            }

            return Ok(new { message = M("OrderCancelled"), orderId = id });
        }

        [HttpGet("schedules/{scheduleId}/has-orders")]
        [AllowAnonymous] // Internal call từ TourAPI, không cần user token
        public async Task<IActionResult> CheckScheduleHasOrders(int scheduleId)
        {
            if (scheduleId <= 0)
                return BadRequest(new { message = "Invalid scheduleId." });

            // Dùng lại CheckBookingAsync đã có sẵn trong service
            var hasOrders = await _orderService.CheckBookingAsync(new CheckBookingTour
            {
                ScheduleIds = new List<int> { scheduleId }
            });

            return Ok(new
            {
                hasOrders,
                scheduleId
            });
        }

        [HttpPost("{id}/internal-payment-result")]
        [AllowAnonymous]
        public async Task<IActionResult> ApplyInternalPaymentResult(
            int id,
            [FromBody] InternalPaymentResultDTO request,
            [FromServices] IConfiguration configuration)
        {
            var configuredKey = configuration["InternalService:Key"];
            var providedKey = Request.Headers["X-StayHub-Service-Key"].ToString();
            if (!KeysMatch(configuredKey, providedKey))
            {
                return Unauthorized(new { message = "Invalid internal service key." });
            }

            var updated = request.IsSuccess
                ? await _orderService.MarkOrderPaidAsync(
                    id,
                    request.CustomerEmail ?? string.Empty)
                : await _orderService.CancelOrderAsync(id);

            if (!updated)
            {
                return BadRequest(new { message = "Order payment result could not be applied." });
            }

            return Ok(new
            {
                orderId = id,
                status = request.IsSuccess ? "Paid" : "Cancelled"
            });
        }

        [HttpGet("schedules/{scheduleId}/customer-ids")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomerIdsByScheduleId(int scheduleId)
        {
            if (scheduleId <= 0)
                return BadRequest(new { message = "Invalid scheduleId." });

            var customerIds = await _orderService.GetCustomerIdsByScheduleIdAsync(scheduleId);
            return Ok(new { data = customerIds });
        }

        [HttpGet("users/{userId}/eligible-schedule-ids")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEligibleScheduleIdsInternal(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid userId." });

            var scheduleIds = await _orderService.GetEligibleScheduleIdsByUserIdAsync(userId);
            return Ok(new { data = scheduleIds });
        }

        private static bool KeysMatch(string? configuredKey, string? providedKey)
        {
            if (string.IsNullOrWhiteSpace(configuredKey) ||
                string.IsNullOrWhiteSpace(providedKey))
            {
                return false;
            }

            var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
            var providedBytes = Encoding.UTF8.GetBytes(providedKey);
            return configuredBytes.Length == providedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(
                       configuredBytes,
                       providedBytes);
        }
    }
}

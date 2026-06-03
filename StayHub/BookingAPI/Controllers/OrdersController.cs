using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BookingAPI.DTOs;
using BookingAPI.Exceptions;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : LocalizedControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_orderService = orderService;
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

            try
            {
                var result = await _orderService.CreateOrderAsync(customerId, request);
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
        public async Task<IActionResult> GetCustomersByScheduleId(int scheduleId)
        {
            var result = await _orderService.GetScheduleCustomersAsync(scheduleId);

            if (result == null || !result.Any())
            {
                return Ok(new { message = $"No customers found for Schedule ID {scheduleId}.", data = new List<ScheduleCustomerDTO>() });
            }

            return Ok(new { message = M("ScheduleCustomersRetrievedSuccessfully"), data = result });
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByUserId(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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

            var result = await _orderService.GetOrdersByUserIdAsync(userId, page, pageSize);

            if (result == null || result.Data == null || !result.Data.Any())
            {
                return Ok(new { message = $"No orders found for User ID {userId}.", data = new PaginationDTO<ReadOrderDTO> { Data = new List<ReadOrderDTO>(), Total = 0, TotalPages = 0, CurrentPage = page, PageSize = pageSize } });
            }

            return Ok(new { message = M("OrdersRetrievedSuccessfully"), data = result });
        }

        [HttpPatch("{id}/mark-paid")]
        [Authorize]
        public async Task<IActionResult> MarkOrderPaid(int id)
        {
            var customerEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                                ?? User.FindFirst(ClaimTypes.Email)?.Value
                                ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                return Unauthorized(new { message = M("InvalidTokenClaimsEmailNotFound") });
            }

            var updated = await _orderService.MarkOrderPaidAsync(id, customerEmail);
            if (!updated)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            return Ok(new { message = M("OrderMarkedAsPaid"), orderId = id });
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
    }
}

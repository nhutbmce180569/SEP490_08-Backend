using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PaymentAPI.DTOs;
using PaymentAPI.Services;

namespace PaymentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MomoController : ControllerBase
    {
        private readonly IMomoService _momoService;
        private readonly IConfiguration _configuration;

        public MomoController(IMomoService momoService, IConfiguration configuration)
        {
            _momoService = momoService;
            _configuration = configuration;
        }

        [HttpPost("create-payment")]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreateTransactionDTO transactionDto)
        {
            if (transactionDto.Amount <= 0)
            {
                return BadRequest("Invalid amount");
            }

            var paymentUrl = await _momoService.CreatePaymentUrlAsync(transactionDto);
            return Ok(new { paymentUrl });
        }

        [HttpGet("momo-return")]
        public async Task<IActionResult> MomoReturn()
        {
            return await HandleReturnAsync(Request.Query);
        }

        [HttpPost("momo-return")]
        public async Task<IActionResult> MomoNotify()
        {
            if (!Request.HasFormContentType)
            {
                return BadRequest(new { message = "Invalid MoMo callback payload." });
            }

            var values = Request.Form.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.OrdinalIgnoreCase);

            var result = await _momoService.HandleMomoReturnAsync(new QueryCollection(values));
            if (result.Status != "Success")
            {
                return BadRequest(new { message = result.Status, orderId = result.OrderId });
            }

            return Ok(new { message = "MoMo payment notification processed.", orderId = result.OrderId });
        }

        [HttpPost("confirm/{orderId}")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var confirmed = await _momoService.ConfirmOrderPaymentAsync(orderId);
            if (!confirmed)
            {
                return BadRequest(new { message = "MoMo payment not completed or order could not be updated." });
            }

            return Ok(new { message = "Order payment confirmed via MoMo.", orderId });
        }

        [HttpPost("cancel/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelPayment(int orderId)
        {
            var cancelled = await _momoService.CancelOrderPaymentAsync(orderId);
            if (!cancelled)
            {
                return BadRequest(new { message = "MoMo payment was already completed or order could not be cancelled." });
            }

            return Ok(new { message = "Order cancelled due to MoMo payment cancellation.", orderId });
        }

        private async Task<IActionResult> HandleReturnAsync(IQueryCollection query)
        {
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                                  ?? "http://localhost:5173";

            var result = await _momoService.HandleMomoReturnAsync(query);

            if (result.Status != "Success" || string.IsNullOrEmpty(result.OrderId))
            {
                if (!string.IsNullOrEmpty(result.OrderId))
                {
                    return Redirect($"{frontendBaseUrl}/my-bookings/{result.OrderId}?payment=cancelled");
                }

                return Redirect($"{frontendBaseUrl}/my-bookings?payment=cancelled");
            }

            return Redirect($"{frontendBaseUrl}/my-bookings/{result.OrderId}?payment=success");
        }
    }
}

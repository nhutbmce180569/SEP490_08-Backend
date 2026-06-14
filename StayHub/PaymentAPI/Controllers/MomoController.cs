using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentAPI.DTOs;
using PaymentAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

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
            if (transactionDto.Amount < 1_000 || transactionDto.Amount > 50_000_000)
            {
                return BadRequest("MoMo amount must be between 1,000 and 50,000,000 VND.");
            }
            transactionDto.CustomerEmail =
                User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            var payment = await _momoService.CreatePaymentAsync(transactionDto);
            return Ok(new
            {
                paymentUrl = payment.PaymentUrl,
                deeplink = payment.Deeplink,
                qrCodeUrl = payment.QrCodeUrl
            });
        }

        [HttpGet("momo-return")]
        public async Task<IActionResult> MomoReturn()
        {
            return await HandleReturnAsync(Request.Query);
        }

        [HttpPost("momo-return")]
        public async Task<IActionResult> MomoNotify(
            [FromBody] Dictionary<string, JsonElement> payload)
        {
            var values = payload.ToDictionary(
                item => item.Key,
                item => JsonElementToString(item.Value),
                StringComparer.OrdinalIgnoreCase);

            var result = await _momoService.HandleMomoReturnAsync(values);
            if (string.IsNullOrEmpty(result.OrderId))
            {
                return BadRequest(new { message = result.Status, orderId = result.OrderId });
            }

            return Ok(new
            {
                message = "MoMo payment notification processed.",
                orderId = result.OrderId,
                status = result.Status
            });
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
            var values = query.ToDictionary(
                item => item.Key,
                item => item.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);
            var result = await _momoService.HandleMomoReturnAsync(values);

            var status = result.Status == "Success" ? "success" : "cancelled";
            if (result.ClientType == "mobile")
            {
                return Redirect(
                    $"stayhub://payment-result?orderId={result.OrderId}&status={status}&provider=momo");
            }

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                                  ?? "http://localhost:5173";
            return string.IsNullOrEmpty(result.OrderId)
                ? Redirect($"{frontendBaseUrl}/my-bookings?payment={status}&provider=momo")
                : Redirect(
                    $"{frontendBaseUrl}/my-bookings/{result.OrderId}?payment={status}&provider=momo");
        }

        private static string JsonElementToString(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : element.GetRawText();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using PaymentAPI.DTOs;
using PaymentAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PaymentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNPayController : LocalizedControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;

        public VNPayController(IVnPayService vnPayService, IConfiguration configuration, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_vnPayService = vnPayService;
            _configuration = configuration;
        }

        [HttpPost("create-payment")]
        [Authorize]
        public async Task<ActionResult<string>> CreatePayment([FromBody] CreateTransactionDTO transactionDto)
        {
            if (transactionDto.Amount <= 0) return BadRequest("Invalid amount");
            transactionDto.CustomerEmail =
                User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4()?.ToString()
                            ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var paymentUrl = await _vnPayService.CreatePaymentUrl(transactionDto, ipAddress);

            return Ok(new { paymentUrl });
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var query = Request.Query;
            string rawQuery = Request.QueryString.Value ?? string.Empty;
            var result = await _vnPayService.HandleVnPayReturnAsync(query, rawQuery);
            return RedirectToClient(result, "vnpay");
        }

        /// <summary>
        /// Fallback: sync order status when user lands on order detail after payment.
        /// </summary>
        [HttpPost("confirm/{orderId}")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var confirmed = await _vnPayService.ConfirmOrderPaymentAsync(orderId);
            if (!confirmed)
            {
                return BadRequest(new { message = M("PaymentNotCompletedOrOrderCouldNotBeUpdated") });
            }

            return Ok(new { message = M("OrderPaymentConfirmed"), orderId });
        }

        [HttpPost("cancel/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelPayment(int orderId)
        {
            var cancelled = await _vnPayService.CancelOrderPaymentAsync(orderId);
            if (!cancelled)
            {
                return BadRequest(new { message = M("PaymentWasAlreadyCompletedOrOrderCouldNotBeCancelled") });
            }

            return Ok(new { message = M("OrderCancelledDueToPaymentCancellation"), orderId });
        }

        private IActionResult RedirectToClient(
            PaymentCallbackResultDTO result,
            string provider)
        {
            var status = result.Status == "Success" ? "success" : "cancelled";
            if (result.ClientType == "mobile")
            {
                return Redirect(
                    $"stayhub://payment-result?orderId={result.OrderId}&status={status}&provider={provider}");
            }

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/')
                                  ?? "http://localhost:5173";
            return string.IsNullOrEmpty(result.OrderId)
                ? Redirect($"{frontendBaseUrl}/my-bookings?payment={status}&provider={provider}")
                : Redirect(
                    $"{frontendBaseUrl}/my-bookings/{result.OrderId}?payment={status}&provider={provider}");
        }
    }
}

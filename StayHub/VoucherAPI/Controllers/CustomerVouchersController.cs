using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using VoucherAPI.DTOs;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

[Authorize(Roles = "Customer")]
[Route("api/customer/vouchers")]
[ApiController]
public class CustomerVouchersController : LocalizedControllerBase
{
    private readonly ICustomerVoucherService _customerVoucherService;

    public CustomerVouchersController(
        ICustomerVoucherService customerVoucherService,
        IStringLocalizer<Messages> localizer)
        : base(localizer)
    {
        _customerVoucherService = customerVoucherService;
    }

    [HttpPost]
    public async Task<IActionResult> SaveVoucher([FromBody] SaveVoucherDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var result = await _customerVoucherService.SaveVoucherAsync(userId.Value, dto.Code);
            return CreatedAtAction(nameof(GetMySavedVouchers), result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMySavedVouchers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var result = await _customerVoucherService.GetMySavedVouchersAsync(userId.Value, page, pageSize, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var result = await _customerVoucherService.ApplyVoucherAsync(userId.Value, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemVoucher([FromBody] ApplyVoucherDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var result = await _customerVoucherService.RedeemVoucherAsync(userId.Value, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}

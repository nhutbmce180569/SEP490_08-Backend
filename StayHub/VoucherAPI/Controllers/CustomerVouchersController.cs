using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoucherAPI.DTOs;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

[Authorize]
[Route("api/customer/vouchers")]
[ApiController]
public class CustomerVouchersController : ControllerBase
{
    private readonly ICustomerVoucherService _customerVoucherService;

    public CustomerVouchersController(ICustomerVoucherService customerVoucherService)
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

        try
        {
            var userId = GetCurrentUserId();
            var result = await _customerVoucherService.SaveVoucherAsync(userId, dto.Code);
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
        try
        {
            var userId = GetCurrentUserId();
            var result = await _customerVoucherService.GetMySavedVouchersAsync(userId, page, pageSize, status);
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

        try
        {
            var userId = GetCurrentUserId();
            var result = await _customerVoucherService.ApplyVoucherAsync(userId, dto);
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

        try
        {
            var userId = GetCurrentUserId();
            var result = await _customerVoucherService.RedeemVoucherAsync(userId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new Exception("Cannot extract user ID from token.");
        }

        return userId;
    }
}

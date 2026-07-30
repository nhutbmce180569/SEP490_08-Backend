using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using VoucherAPI.DTOs;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

[AllowAnonymous]
[Route("api/internal/vouchers")]
[ApiController]
public class InternalVouchersController : LocalizedControllerBase
{
    private readonly ICustomerVoucherService _customerVoucherService;
    private readonly IConfiguration _configuration;

    public InternalVouchersController(
        ICustomerVoucherService customerVoucherService,
        IConfiguration configuration, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_customerVoucherService = customerVoucherService;
        _configuration = configuration;
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreVoucher([FromBody] RestoreVoucherDTO dto)
    {
        if (!IsValidServiceKey())
        {
            return Unauthorized(new { message = M("InvalidServiceKey") });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _customerVoucherService.RestoreVoucherAsync(dto.CustomerId, dto.Code);
            return Ok(new { message = M("VoucherRestoredSuccessfully") });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsValidServiceKey()
    {
        var expectedKey = _configuration["InternalService:Key"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue("X-Service-Key", out var providedKey))
        {
            return false;
        }

        return string.Equals(expectedKey, providedKey.ToString(), StringComparison.Ordinal);
    }
}

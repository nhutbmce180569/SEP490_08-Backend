using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using VoucherAPI.DTOs;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

[Route("api/vouchers")]
[ApiController]
[Authorize(Roles = "Manager,Admin")]
public class VouchersController : LocalizedControllerBase
{
    private readonly IVoucherService _voucherService;

    public VouchersController(IVoucherService voucherService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_voucherService = voucherService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationDTO<ReadVoucherDTO>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? tourId = null,
        [FromQuery] string? discountType = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? createdByMe = null,
        [FromQuery] string? voucherType = null)
    {
        var result = await _voucherService.GetAll(page, pageSize, search, tourId, discountType, status, isActive, createdByMe, GetCurrentUserId() ?? 0, voucherType);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReadVoucherDetailDTO>> GetById(int id)
    {
        var result = await _voucherService.GetById(id);
        if (result == null)
        {
            return NotFound(new { message = M("VoucherNotFound") });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReadVoucherDetailDTO>> Create([FromBody] CreateVoucherDTO dto)
    {
        dto.Code = dto.Code.Trim().ToUpperInvariant();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var creatorId = GetCurrentUserId();
        if (creatorId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var isAdmin = User.IsInRole("Admin");
            var result = await _voucherService.Create(dto, creatorId.Value, isAdmin);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReadVoucherDetailDTO>> Update(int id, [FromBody] UpdateVoucherDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var isAdmin = User.IsInRole("Admin");
            var result = await _voucherService.Update(id, dto, currentUserId.Value, isAdmin);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message == "Voucher not found")
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<ReadVoucherDTO>> Activate(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var isAdmin = User.IsInRole("Admin");
            var result = await _voucherService.Activate(id, currentUserId.Value, isAdmin);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message == "Voucher not found")
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<ReadVoucherDTO>> Deactivate(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        try
        {
            var isAdmin = User.IsInRole("Admin");
            var result = await _voucherService.Deactivate(id, currentUserId.Value, isAdmin);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message == "Voucher not found")
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("birthday-distribute/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CheckBirthdayVoucherStatus([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var isDistributed = await _voucherService.CheckBirthdayVoucherDistributedAsync(month, year);
            return Ok(new { isDistributed });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("birthday-distribute")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DistributeBirthdayVoucher([FromQuery] int? month = null)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = M("CannotExtractUserIDFromToken") });
        }

        var targetMonth = month ?? DateTime.Now.Month;

        try
        {
            var result = await _voucherService.DistributeBirthdayVoucherAsync(targetMonth, currentUserId.Value);
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

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

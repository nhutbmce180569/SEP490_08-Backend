using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoucherAPI.DTOs;
using VoucherAPI.Services;

namespace VoucherAPI.Controllers;

[Route("api/vouchers")]
[ApiController]
[Authorize(Roles = "Manager")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;

    public VouchersController(IVoucherService voucherService)
    {
        _voucherService = voucherService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationDTO<ReadVoucherDTO>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? tourId = null,
        [FromQuery] string? discountType = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _voucherService.GetAll(page, pageSize, search, tourId, discountType, status, isActive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReadVoucherDetailDTO>> GetById(int id)
    {
        var result = await _voucherService.GetById(id);
        if (result == null)
        {
            return NotFound(new { message = "Voucher not found" });
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
            return Unauthorized(new { message = "Cannot extract user ID from token" });
        }

        try
        {
            var result = await _voucherService.Create(dto, creatorId.Value);
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

        try
        {
            var result = await _voucherService.Update(id, dto);
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
        try
        {
            var result = await _voucherService.Activate(id);
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
        try
        {
            var result = await _voucherService.Deactivate(id);
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

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("id")?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using SocialAPI.DTOs;
using SocialAPI.Services;

namespace SocialAPI.Controllers;

[Route("api/moderation")]
[ApiController]
[Authorize]
public class ModerationController : LocalizedControllerBase
{
    private readonly IMomentService _momentService;

    public ModerationController(IMomentService momentService, IStringLocalizer<Messages> localizer)
        : base(localizer)
    {
        _momentService = momentService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("id")?.Value;

        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in token.");
    }

    private bool IsAuthorizedModerator()
    {
        var roles = User.FindAll(ClaimTypes.Role)
                        .Concat(User.FindAll("role"))
                        .Select(r => r.Value.ToLowerInvariant())
                        .ToList();

        return roles.Contains("staff") || roles.Contains("tourmanager") || roles.Contains("manager") || roles.Contains("admin");
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportContent([FromBody] ReportRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _momentService.ReportContentAsync(userId, dto.ContentType, dto.TargetId, dto.Reason, dto.Details);
            return Ok(new { message = M("ReportSubmittedSuccessfully") ?? "Báo cáo nội dung đã được gửi thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while submitting the report.", details = ex.Message });
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetPendingReports()
    {
        try
        {
            if (!IsAuthorizedModerator())
            {
                return StatusCode(403, new { message = M("AccessDenied") ?? "Bạn không có quyền truy cập chức năng này." });
            }

            var reports = await _momentService.GetPendingReportsAsync();
            return Ok(reports);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching reports.", details = ex.Message });
        }
    }

    [HttpPost("reports/{id}/resolve")]
    public async Task<IActionResult> ResolveReport(int id, [FromQuery] string action)
    {
        try
        {
            if (!IsAuthorizedModerator())
            {
                return StatusCode(403, new { message = M("AccessDenied") ?? "Bạn không có quyền truy cập chức năng này." });
            }

            var userId = GetCurrentUserId();
            await _momentService.ResolveReportAsync(id, action, userId);
            return Ok(new { message = M("ReportResolvedSuccessfully") ?? "Báo cáo đã được xử lý thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while resolving the report.", details = ex.Message });
        }
    }
}

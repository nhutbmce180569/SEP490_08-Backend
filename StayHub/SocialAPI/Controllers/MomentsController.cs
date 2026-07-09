using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using SocialAPI.DTOs;
using SocialAPI.Services;
using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MomentsController : LocalizedControllerBase
{
    private readonly IMomentService _momentService;

    public MomentsController(IMomentService momentService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_momentService = momentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMoment([FromForm] MomentCreateDto dto)
    {
        try
        {
            var result = await _momentService.CreateMomentAsync(dto);
            return StatusCode(201, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileCreatingTheMoment"), details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet]
    // 💡 SỬA TẠI ĐÂY: Đổi int scheduleId thành int? scheduleId
    public async Task<IActionResult> GetMomentFeed([FromQuery] int? scheduleId, [FromQuery(Name = "$skip")] int skip = 0, [FromQuery(Name = "$top")] int top = 5)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("UserIDNotFoundInToken") });
            }

            // Truyền scheduleId (nullable) xuống Service
            var result = await _momentService.GetMomentFeedWithUsersAsync(scheduleId, userId, skip, top);

            Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
            Response.Headers.Add("Pragma", "no-cache");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileFetchingMoments"), details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-footprints")]
    public async Task<IActionResult> GetMyFootprints()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("UserIDNotFoundInToken") });
            }

            var result = await _momentService.GetMyFootprintsAsync(userId);
            return Ok(new { message = M("FootprintsRetrievedSuccessfully"), data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingFootprints"), details = ex.Message });
        }
    }

    [HttpPost("{id}/reactions")]
    public async Task<IActionResult> ToggleReaction(int id, [FromBody] ReactionRequestDto dto)
    {
        try
        {
            await _momentService.ToggleReactionAsync(id, dto);
            return Ok(new { message = M("ReactionToggledSuccessfully") });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileTogglingTheReaction"), details = ex.Message });
        }
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] CommentRequestDto dto)
    {
        try
        {
            var result = await _momentService.AddCommentAsync(id, dto);
            return StatusCode(201, result);
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
            return StatusCode(500, new { message = M("AnErrorOccurredWhileAddingTheComment"), details = ex.Message });
        }
    }

    [HttpPut("comments/{cId}")]
    public async Task<IActionResult> UpdateComment(int cId, [FromBody] CommentRequestDto dto)
    {
        try
        {
            var result = await _momentService.UpdateCommentAsync(cId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileUpdatingTheComment"), details = ex.Message });
        }
    }

    [HttpDelete("comments/{cId}")]
    public async Task<IActionResult> DeleteComment(int cId, [FromQuery] int userId)
    {
        try
        {
            await _momentService.DeleteCommentAsync(cId, userId);
            return Ok(new { message = M("CommentDeletedSuccessfully") });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileDeletingTheComment"), details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMoment(int id, [FromQuery] int userId)
    {
        try
        {
            await _momentService.DeleteMomentAsync(id, userId);
            return Ok(new { message = M("MomentDeletedSuccessfully") });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileDeletingTheMoment"), details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("user/{targetUserId}")]
    public async Task<IActionResult> GetUserMoments(int targetUserId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = M("UserIDNotFoundInToken") });
            }

            var result = await _momentService.GetUserMomentsAsync(targetUserId, currentUserId);

            return Ok(new { message = M("UserMomentsRetrievedSuccessfully"), data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileFetchingUserMoments"), details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMomentById(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("UserIDNotFoundInToken") });
            }

            var result = await _momentService.GetMomentByIdAsync(id, userId);
            if (result == null) return NotFound(new { message = "Moment not found." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileFetchingMoments"), details = ex.Message });
        }
    }
}
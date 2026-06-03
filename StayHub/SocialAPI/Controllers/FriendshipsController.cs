using System;
using System.Collections.Generic;
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

[Route("api/friends")]
[ApiController]
[Authorize]
public class FriendshipsController : LocalizedControllerBase
{
    private readonly IFriendshipService _friendshipService;

    public FriendshipsController(IFriendshipService friendshipService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_friendshipService = friendshipService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in token.");
    }

    [HttpPost]
    public async Task<IActionResult> SendRequest([FromBody] FriendRequestDto requestDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _friendshipService.SendRequestAsync(userId, requestDto);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFriendships()
    {
        var userId = GetCurrentUserId();
        var friendships = await _friendshipService.GetFriendshipsAsync(userId);
        return Ok(friendships);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userId = GetCurrentUserId();
        var requests = await _friendshipService.GetPendingRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpPut("respond")]
    public async Task<IActionResult> RespondToRequest([FromBody] FriendRequestUpdateDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _friendshipService.UpdateRequestStatusAsync(userId, updateDto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFriendship(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _friendshipService.DeleteFriendshipAsync(userId, id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetFriendList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = M("UserIDNotFoundInToken") });
            }

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var result = await _friendshipService.GetFriendListAsync(userId, page, pageSize);

            if (result.Total == 0)
            {
                return Ok(new 
                { 
                    message = M("YouDonTHaveAnyFriendsInYourListYet"), 
                    data = result 
                });
            }

            return Ok(new 
            { 
                message = $"Successfully retrieved {result.Data.Count()} friend(s).", 
                data = result 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingFriendList"), details = ex.Message });
        }
    }
}
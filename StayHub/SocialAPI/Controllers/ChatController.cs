using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAPI.DTOs;
using SocialAPI.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SocialAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("UserId")?.Value
                ?? User.FindFirst("Id")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Invalid user token.");
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetUserChatRooms()
        {
            try
            {
                var userId = GetUserId();
                var rooms = await _chatService.GetUserChatRoomsAsync(userId);
                return Ok(rooms);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving chat rooms.", error = ex.Message });
            }
        }

        [HttpGet("rooms/{roomId}/messages")]
        public async Task<IActionResult> GetChatHistory(int roomId, [FromQuery] int skip = 0, [FromQuery] int top = 20)
        {
            try
            {
                var userId = GetUserId();
                var messages = await _chatService.GetChatHistoryAsync(roomId, userId, skip, top);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(); // Using Forbid when user is authenticated but doesn't have access to this room
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving chat history.", error = ex.Message });
            }
        }

        [HttpPost("rooms")]
        public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomRequest request)
        {
            try
            {
                // Viết logic bóc tách currentUserId từ JWT Token an toàn (ClaimTypes.NameIdentifier / sub / id)
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("sub")?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                var room = await _chatService.CreateOrGetChatRoomAsync(userId, request.FriendId);
                return Ok(room);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the chat room.", error = ex.Message });
            }
        }

        [HttpPost("rooms/{roomId}/pin")]
        public async Task<IActionResult> TogglePin(int roomId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatService.TogglePinChatAsync(roomId, userId);
                return Ok(new { isPinned = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while pinning the chat room.", error = ex.Message });
            }
        }

        [HttpPost("rooms/{roomId}/mute")]
        public async Task<IActionResult> ToggleMute(int roomId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatService.ToggleMuteChatAsync(roomId, userId);
                return Ok(new { isMuted = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while muting the chat room.", error = ex.Message });
            }
        }

        [HttpPost("rooms/{roomId}/members")]
        public async Task<IActionResult> AddMembers(int roomId, [FromBody] AddMembersRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatService.AddMembersToRoomAsync(roomId, userId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding members.", error = ex.Message });
            }
        }

        [HttpDelete("rooms/{roomId}/leave")]
        public async Task<IActionResult> LeaveChatRoom(int roomId)
        {
            try
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("sub")?.Value
                                   ?? User.FindFirst("id")?.Value;

                if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

                await _chatService.LeaveRoomAsync(roomId, userId);
                return Ok(new { message = "Left the chat room successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error leaving room", details = ex.Message });
            }
        }
    }
}

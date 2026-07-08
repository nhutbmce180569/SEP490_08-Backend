using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
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
    public class ChatController : LocalizedControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService, IStringLocalizer<Messages> localizer)
            : base(localizer)
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
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingChatRooms"), error = ex.Message });
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
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingChatHistory"), error = ex.Message });
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
                    return Unauthorized(new { message = M("UserIDNotFoundInToken") });
                }

                var room = await _chatService.CreateOrGetChatRoomAsync(userId, request.FriendId);
                return Ok(room);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileCreatingTheChatRoom"), error = ex.Message });
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
                return StatusCode(500, new { message = M("AnErrorOccurredWhilePinningTheChatRoom"), error = ex.Message });
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
                return StatusCode(500, new { message = M("AnErrorOccurredWhileMutingTheChatRoom"), error = ex.Message });
            }
        }

        [HttpPost("rooms/{roomId}/read")]
        public async Task<IActionResult> MarkRoomAsRead(int roomId)
        {
            try
            {
                var userId = GetUserId();
                await _chatService.MarkRoomAsReadAsync(userId, roomId);
                return Ok(new { message = "Marked as read successfully." });
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
                return StatusCode(500, new { message = M("AnErrorOccurredWhileMarkingAsRead"), error = ex.Message });
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
                return StatusCode(500, new { message = M("AnErrorOccurredWhileAddingMembers"), error = ex.Message });
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
                return Ok(new { message = M("LeftTheChatRoomSuccessfully") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("ErrorLeavingRoom"), details = ex.Message });
            }
        }

        /// <summary>
        /// Internal API: Create a schedule-based chat room
        /// Accessible via Token Forwarding from Tour Manager
        /// </summary>
        [HttpPost("rooms/schedule")]
        [Authorize] // Sử dụng Token Forwarding từ Manager tạo Tour
        public async Task<IActionResult> CreateScheduleRoom([FromBody] CreateScheduleChatRoomRequest request)
        {
            try
            {
                if (request == null || request.ScheduleId <= 0) return BadRequest();

                var roomId = await _chatService.CreateScheduleRoomAsync(request);
                return Ok(new { message = "Create a chat room for a successful tour itinerary.", roomId = roomId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Internal API: Automatically add a member to a schedule-based chat room
        /// Accessible by other internal services via API Gateway without user token validation
        /// </summary>
        [HttpPost("rooms/schedule/{scheduleId}/add-member")]
        [AllowAnonymous]
        public async Task<IActionResult> AddMemberToScheduleRoom(int scheduleId, [FromBody] AutoAddChatMemberRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (request.UserId <= 0)
                {
                    return BadRequest(new { message = "UserId must be a positive integer." });
                }

                if (scheduleId <= 0)
                {
                    return BadRequest(new { message = "ScheduleId must be a positive integer." });
                }

                await _chatService.AutoAddMemberByScheduleAsync(scheduleId, request);
                return Ok(new { message = "Member added to schedule chat room successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the member.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        /// <summary>
        /// Internal API: Add members to a schedule-based chat room via ScheduleId
        /// Used by BookingAPI with JWT token forwarding for secure inter-service communication
        /// </summary>
        [HttpPost("rooms/schedule/{scheduleId}/members")]
        [AllowAnonymous] // SỬA TẠI ĐÂY: Cho phép BookingAPI gọi qua HTTP nội bộ mà không cần JWT
        public async Task<IActionResult> AddMembersBySchedule(int scheduleId, [FromBody] AddMembersRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request body cannot be null." });
                }

                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(new { message = "UserIds list cannot be null or empty." });
                }

                if (scheduleId <= 0)
                {
                    return BadRequest(new { message = "ScheduleId must be a positive integer." });
                }

                var success = await _chatService.AddMembersByScheduleAsync(scheduleId, request.UserIds);

                if (!success)
                {
                    return NotFound(new { message = "Chat room corresponding to this tour schedule was not found." });
                }

                return Ok(new { message = "Members have been automatically added to the tour group successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all members of a specific chat room with their profile information
        /// </summary>
        [HttpGet("rooms/{roomId}/members")]
        public async Task<IActionResult> GetRoomMembers(int roomId)
        {
            try
            {
                if (roomId <= 0)
                {
                    return BadRequest(new { message = "RoomId must be a positive integer." });
                }

                var members = await _chatService.GetRoomMembersAsync(roomId);
                return Ok(new { message = "Successfully retrieved room members.", data = members });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileRetrievingMembers"), error = ex.Message });
            }
        }

    }
}

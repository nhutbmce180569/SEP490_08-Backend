using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SystemAPI.DTOs;
using SystemAPI.Services;

namespace SystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ==========================================
        // 1. ENDPOINT NỘI BỘ (Cho BookingAPI gọi sang)
        // ==========================================
        // Lưu ý: Trong thực tế, endpoint này nên được bảo vệ bằng API Key 
        // hoặc Role nội bộ để tránh việc user bên ngoài tự ý bắn request rác.
        [HttpPost("internal/send")]
        public async Task<IActionResult> SendInternalNotification([FromBody] CreateNotificationDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _notificationService.CreateAndSendNotificationAsync(dto);
            return Ok(result); // Trả về thông báo vừa tạo (và lúc này SignalR cũng đã bắn ngầm xong)
        }

        // ==========================================
        // 2. ENDPOINT CHO FRONTEND (Lấy danh sách thông báo)
        // ==========================================
        [Authorize] // Bắt buộc phải có JWT Token
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            // Tự động bóc tách UserId từ JWT Token của người đang đăng nhập
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        // ==========================================
        // 3. ENDPOINT CHO FRONTEND (Đánh dấu đã đọc)
        // ==========================================
        [Authorize]
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Bóc tách UserId để kiểm tra xem người này có đang đọc đúng thông báo của mình không
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            try
            {
                await _notificationService.MarkAsReadAsync(id, userId);
                return NoContent(); // Status 204: Thành công nhưng không cần trả data về
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            try
            {
                // Gọi thẳng vào Service bạn vừa viết
                await _notificationService.DeleteNotificationAsync(id, userId);
                return NoContent(); // Status 204: Xóa thành công
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message); // Status 403 nếu cố tình xóa của người khác
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

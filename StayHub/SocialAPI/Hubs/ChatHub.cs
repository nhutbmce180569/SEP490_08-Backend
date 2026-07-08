using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialAPI.DTOs;
using SocialAPI.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SocialAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IChatNotificationService _chatNotificationService;

        public ChatHub(IChatService chatService, IChatNotificationService chatNotificationService)
        {
            _chatService = chatService;
            _chatNotificationService = chatNotificationService;
        }

        public async Task JoinRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        }

        public async Task LeaveRoom(int roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        }

        public async Task SendMessage(int chatRoomId, string content)
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? Context.User?.FindFirst("sub")?.Value
                               ?? Context.User?.FindFirst("id")?.Value;

            if (!int.TryParse(userIdString, out int senderId))
            {
                throw new HubException("Unauthorized user.");
            }

            var dto = new SendMessageDto
            {
                ChatRoomId = chatRoomId,
                Content = content
            };

            try
            {
                var savedMessage = await _chatService.SaveMessageAsync(senderId, dto);

                await Clients.Group(chatRoomId.ToString()).SendAsync("ReceiveMessage", savedMessage);

                await _chatNotificationService.NotifyNewMessageAsync(savedMessage, senderId);
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }
        }
    }
}

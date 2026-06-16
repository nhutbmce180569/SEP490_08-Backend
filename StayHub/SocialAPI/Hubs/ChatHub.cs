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
        private readonly IHubContext<NotificationHub> _globalHubContext;

        public ChatHub(IChatService chatService, IHubContext<NotificationHub> globalHubContext)
        {
            _chatService = chatService;
            _globalHubContext = globalHubContext;
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

            var savedMessage = await _chatService.SaveMessageAsync(senderId, dto);

            await Clients.Group(chatRoomId.ToString()).SendAsync("ReceiveMessage", savedMessage);

            var members = await _chatService.GetRoomMembersAsync(chatRoomId);
            foreach (var member in members)
            {
                if (member.Id != senderId)
                {
                    await _globalHubContext.Clients.User(member.Id.ToString()).SendAsync("ReceiveGlobalNotification", savedMessage);
                }
            }
        }
    }
}

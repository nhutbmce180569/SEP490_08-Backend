using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SystemAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Có thể log ra xem user nào đang kết nối
            var userId = Context.UserIdentifier;
            await base.OnConnectedAsync();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocialAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Hub này để trống, mục đích để User giữ kết nối toàn cục xuyên suốt hệ thống
    }
}
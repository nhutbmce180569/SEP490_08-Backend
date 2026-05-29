using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocialAPI.Hubs
{
    [Authorize]
    public class FriendshipHub : Hub
    {
    }
}
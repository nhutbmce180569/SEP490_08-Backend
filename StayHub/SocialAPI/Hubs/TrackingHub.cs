using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SocialAPI.Hubs
{
    public class TrackingHub : Hub
    {
        public async Task JoinTrackingGroup(string token)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, token);
        }
    }
}
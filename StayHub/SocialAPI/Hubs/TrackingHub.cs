using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SocialAPI.Hubs
{
    [Authorize]
    public class TrackingHub : Hub
    {
        private const string TourGroupPrefix = "tour_";

        [AllowAnonymous]
        public async Task JoinTrackingGroup(string token)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, token);
        }

        public async Task JoinTourTrackingGroup(int scheduleId)
        {
            if (scheduleId <= 0)
            {
                throw new HubException("Invalid schedule identifier.");
            }

            if (Context.User?.Identity == null || !Context.User.Identity.IsAuthenticated)
            {
                throw new HubException("Authentication is required to join tour tracking.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"{TourGroupPrefix}{scheduleId}");
        }
    }
}
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SocialAPI.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.FindFirst("sub")?.Value
                ?? connection.User?.FindFirst("id")?.Value;
        }
    }
}
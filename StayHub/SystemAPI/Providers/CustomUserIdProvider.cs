using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SystemAPI.Providers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Map đúng với claim trong JWT của bạn (thường là ClaimTypes.NameIdentifier hoặc "id")
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.FindFirst("sub")?.Value
                ?? connection.User?.FindFirst("id")?.Value;
        }
    }
}

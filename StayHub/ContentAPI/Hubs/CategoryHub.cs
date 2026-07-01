using Microsoft.AspNetCore.SignalR;

namespace ContentAPI.Hubs
{
    public class CategoryHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}

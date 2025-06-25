// Hubs/DashboardHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace TinVangNews.Hubs
{

    [Authorize]
    public class DashboardHub : Hub
    {

        public override async Task OnConnectedAsync()
        {
            // Nếu người dùng kết nối có vai trò là "Admin"...
            if (Context.User.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {

            await base.OnDisconnectedAsync(exception);
        }
    }
}
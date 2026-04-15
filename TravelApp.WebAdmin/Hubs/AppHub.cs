using Microsoft.AspNetCore.SignalR;
using TravelApp.WebAdmin.Data;

namespace TravelApp.WebAdmin.Hubs
{
    public class AppHub : Hub
    {
        private readonly AppDbContext _context;
        public AppHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateUserStatus(int userId, bool isOnline)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = isOnline;
                await _context.SaveChangesAsync();
            }

            await Clients.All.SendAsync("ReceiveUserStatus", userId, isOnline);
        }

        public async Task NotifyProfileUpdated(int userId)
        {
            await Clients.All.SendAsync("ReceiveProfileUpdate", userId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
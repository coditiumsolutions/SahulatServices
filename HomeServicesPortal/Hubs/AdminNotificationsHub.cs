using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HomeServicesPortal.Hubs;

/// <summary>Real-time admin portal notifications (cookie-authenticated staff).</summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class AdminNotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminNotificationService.AdminsGroup);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminNotificationService.AdminsGroup);
        await base.OnDisconnectedAsync(exception);
    }
}

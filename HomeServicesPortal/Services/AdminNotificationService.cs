using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Hubs;
using HomeServicesPortal.Models.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class AdminNotificationService : IAdminNotificationService
{
    public const string ServiceRequestCreated = "ServiceRequestCreated";
    public const string AdminsGroup = "admins";

    private readonly AppDbContext _db;
    private readonly IHubContext<AdminNotificationsHub> _hub;
    private readonly ILogger<AdminNotificationService> _logger;

    public AdminNotificationService(
        AppDbContext db,
        IHubContext<AdminNotificationsHub> hub,
        ILogger<AdminNotificationService> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyServiceRequestCreatedAsync(
        int requestUid,
        string serviceTitle,
        string? clientName,
        CancellationToken cancellationToken = default)
    {
        var title = "New service request";
        var who = string.IsNullOrWhiteSpace(clientName) ? "a customer" : clientName.Trim();
        var safeTitle = (serviceTitle ?? string.Empty).Trim();
        var message = $"{who} submitted '{safeTitle}' (#{requestUid}).";
        if (message.Length > 500)
        {
            message = message[..497] + "...";
        }

        var entity = new AdminNotification
        {
            Type = ServiceRequestCreated,
            Title = title,
            Message = message,
            LinkUrl = $"/Admin/ServiceRequests/Details/{requestUid}",
            RelatedEntityUid = requestUid,
            IsRead = false,
            CreatedOn = DateTime.Now
        };

        _db.AdminNotifications.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = Map(entity);
        var unreadCount = await _db.AdminNotifications.CountAsync(n => !n.IsRead, cancellationToken);

        try
        {
            // SendCoreAsync avoids CancellationToken being mistaken for a client argument.
            await _hub.Clients.Group(AdminsGroup).SendCoreAsync(
                "ReceiveNotification",
                new object[] { new { notification = dto, unreadCount } },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Persist succeeded; live push is best-effort — clients still poll /feed.
            _logger.LogWarning(ex, "Saved admin notification for request {RequestUid} but SignalR push failed.", requestUid);
        }

        _logger.LogInformation(
            "Admin notification {NotificationUid} created for service request {RequestUid}. Unread={UnreadCount}",
            entity.Uid,
            requestUid,
            unreadCount);
    }

    public async Task<AdminNotificationFeedDto> GetRecentAsync(
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = take < 1 ? 20 : Math.Min(take, 50);

        var unreadCount = await _db.AdminNotifications
            .CountAsync(n => !n.IsRead, cancellationToken);

        var items = await _db.AdminNotifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedOn)
            .ThenByDescending(n => n.Uid)
            .Take(take)
            .Select(n => new AdminNotificationDto
            {
                Uid = n.Uid,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                RelatedEntityUid = n.RelatedEntityUid,
                IsRead = n.IsRead,
                CreatedOn = n.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new AdminNotificationFeedDto
        {
            UnreadCount = unreadCount,
            Items = items
        };
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var unread = await _db.AdminNotifications
            .Where(n => !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return 0;
        }

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _hub.Clients.Group(AdminsGroup).SendCoreAsync(
                "NotificationsMarkedRead",
                new object[] { new { unreadCount = 0 } },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Marked notifications read but SignalR push failed.");
        }

        return unread.Count;
    }

    private static AdminNotificationDto Map(AdminNotification n) => new()
    {
        Uid = n.Uid,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        LinkUrl = n.LinkUrl,
        RelatedEntityUid = n.RelatedEntityUid,
        IsRead = n.IsRead,
        CreatedOn = n.CreatedOn
    };
}

using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Hubs;
using HomeServicesPortal.Models.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HomeServicesPortal.Services;

public class AdminNotificationService : IAdminNotificationService
{
    public const string ServiceRequestCreated = "ServiceRequestCreated";
    public const string CustomerRequestCancelled = "CustomerRequestCancelled";
    public const string ProviderBookingCancelled = "ProviderBookingCancelled";
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
        var who = string.IsNullOrWhiteSpace(clientName) ? "a customer" : clientName.Trim();
        var safeTitle = (serviceTitle ?? string.Empty).Trim();
        var message = $"{who} submitted '{safeTitle}' (#{requestUid}).";

        await CreateAndPublishAsync(
            ServiceRequestCreated,
            "New service request",
            message,
            $"/Admin/ServiceRequests/Details/{requestUid}",
            requestUid,
            cancellationToken);
    }

    public async Task NotifyCustomerCancellationAsync(
        int requestUid,
        string serviceTitle,
        string? clientName,
        string? cancelReason,
        CancellationToken cancellationToken = default)
    {
        var who = string.IsNullOrWhiteSpace(clientName) ? "A customer" : clientName.Trim();
        var safeTitle = (serviceTitle ?? string.Empty).Trim();
        var reasonSuffix = string.IsNullOrWhiteSpace(cancelReason) ? string.Empty : $" Reason: {cancelReason.Trim()}.";
        var message = $"{who} cancelled '{safeTitle}' (#{requestUid}).{reasonSuffix}";

        await CreateAndPublishAsync(
            CustomerRequestCancelled,
            "Customer cancelled request",
            message,
            $"/Admin/ServiceRequests/Details/{requestUid}",
            requestUid,
            cancellationToken);
    }

    public async Task NotifyProviderCancellationAsync(
        int bookingUid,
        string? providerName,
        string? cancelReason,
        CancellationToken cancellationToken = default)
    {
        var who = string.IsNullOrWhiteSpace(providerName) ? "A provider" : providerName.Trim();
        var reasonSuffix = string.IsNullOrWhiteSpace(cancelReason) ? string.Empty : $" Reason: {cancelReason.Trim()}.";
        var message = $"{who} cancelled booking #{bookingUid}.{reasonSuffix}";

        await CreateAndPublishAsync(
            ProviderBookingCancelled,
            "Provider cancelled booking",
            message,
            $"/Admin/Bookings/Details/{bookingUid}",
            bookingUid,
            cancellationToken);
    }

    private async Task CreateAndPublishAsync(
        string type,
        string title,
        string message,
        string? linkUrl,
        int? relatedEntityUid,
        CancellationToken cancellationToken)
    {
        if (message.Length > 500)
        {
            message = message[..497] + "...";
        }

        var entity = new AdminNotification
        {
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            RelatedEntityUid = relatedEntityUid,
            IsRead = false,
            CreatedOn = DateTime.Now
        };

        _db.AdminNotifications.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = Map(entity);
        var unreadCount = await _db.AdminNotifications.CountAsync(n => n.Type == type && !n.IsRead, cancellationToken);

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
            _logger.LogWarning(ex, "Saved admin notification {Type} for entity {RelatedEntityUid} but SignalR push failed.", type, relatedEntityUid);
        }

        _logger.LogInformation(
            "Admin notification {NotificationUid} ({Type}) created for entity {RelatedEntityUid}. UnreadForType={UnreadCount}",
            entity.Uid,
            type,
            relatedEntityUid,
            unreadCount);
    }

    public async Task<AdminNotificationFeedDto> GetRecentAsync(
        int take = 20,
        IEnumerable<string>? types = null,
        CancellationToken cancellationToken = default)
    {
        take = take < 1 ? 20 : Math.Min(take, 50);
        var typeList = types?.ToList();

        var query = _db.AdminNotifications.AsNoTracking().AsQueryable();
        if (typeList is { Count: > 0 })
        {
            query = query.Where(n => typeList.Contains(n.Type));
        }

        var unreadCount = await query.CountAsync(n => !n.IsRead, cancellationToken);

        var items = await query
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

    public async Task<int> MarkAllReadAsync(
        IEnumerable<string>? types = null,
        CancellationToken cancellationToken = default)
    {
        var typeList = types?.ToList();

        var query = _db.AdminNotifications.Where(n => !n.IsRead);
        if (typeList is { Count: > 0 })
        {
            query = query.Where(n => typeList.Contains(n.Type));
        }

        var unread = await query.ToListAsync(cancellationToken);

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
                new object[] { new { unreadCount = 0, types = typeList } },
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

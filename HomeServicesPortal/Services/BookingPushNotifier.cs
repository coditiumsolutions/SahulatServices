using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Services;

public class BookingPushNotifier : IBookingPushNotifier
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IOptionsMonitor<NotificationOptions> _options;

    public BookingPushNotifier(
        AppDbContext db,
        INotificationService notifications,
        IOptionsMonitor<NotificationOptions> options)
    {
        _db = db;
        _notifications = notifications;
        _options = options;
    }

    private bool Enabled => _options.CurrentValue.BookingPushEnabled;

    public async Task JobAssignedAsync(int requestUid, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;

        var title = await ServiceTitleAsync(requestUid, cancellationToken);
        var pending = await _db.ServiceBookings.AsNoTracking()
            .Where(b => b.RequestUid == requestUid && b.Status == "Pending")
            .Select(b => new { b.Uid, b.ProviderUid })
            .ToListAsync(cancellationToken);

        var (t, body) = NotificationTemplates.JobAssigned(title);
        foreach (var b in pending)
        {
            await NotifyProviderAsync(b.ProviderUid, NotificationTypes.JobAssigned, t, body,
                NotificationScreens.ProviderJobRequests, b.Uid, requestUid, cancellationToken);
        }
    }

    public async Task BookingAcceptedAsync(
        int bookingUid,
        IReadOnlyCollection<(int BookingUid, int ProviderUid)> losingSiblings,
        CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;

        var info = await LoadBookingAsync(bookingUid, cancellationToken);
        if (info == null) return;

        var (t, body) = NotificationTemplates.BookingAccepted(info.ProviderName, info.ServiceTitle);
        await NotifyClientAsync(info.ClientUid, NotificationTypes.BookingAccepted, t, body,
            NotificationScreens.RequestDetails, bookingUid, info.RequestUid, cancellationToken);

        var (lt, lbody) = NotificationTemplates.JobUnavailable(info.ServiceTitle);
        foreach (var (siblingBookingUid, providerUid) in losingSiblings)
        {
            await NotifyProviderAsync(providerUid, NotificationTypes.JobUnavailable, lt, lbody,
                NotificationScreens.ProviderJobRequests, siblingBookingUid, info.RequestUid, cancellationToken);
        }
    }

    public async Task ProviderReassigningAsync(int requestUid, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;

        var clientUid = await _db.CustomerServiceRequests.AsNoTracking()
            .Where(r => r.Uid == requestUid)
            .Select(r => (int?)r.ClientUid)
            .FirstOrDefaultAsync(cancellationToken);
        if (clientUid == null) return;

        var title = await ServiceTitleAsync(requestUid, cancellationToken);
        var (t, body) = NotificationTemplates.ProviderReassigning(title);
        await NotifyClientAsync(clientUid.Value, NotificationTypes.ProviderReassigning, t, body,
            NotificationScreens.RequestDetails, null, requestUid, cancellationToken);
    }

    public async Task BookingCancelledByStaffAsync(int bookingUid, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;

        var info = await LoadBookingAsync(bookingUid, cancellationToken);
        if (info == null) return;

        var (ct, cbody) = NotificationTemplates.BookingCancelledForClient(info.ServiceTitle, info.CancelReason);
        await NotifyClientAsync(info.ClientUid, NotificationTypes.BookingCancelled, ct, cbody,
            NotificationScreens.RequestDetails, bookingUid, info.RequestUid, cancellationToken);

        var (pt, pbody) = NotificationTemplates.BookingCancelledForProvider(info.ServiceTitle, info.CancelReason);
        await NotifyProviderAsync(info.ProviderUid, NotificationTypes.BookingCancelled, pt, pbody,
            NotificationScreens.MyBookings, bookingUid, info.RequestUid, cancellationToken);
    }

    public async Task JobStartedAsync(int bookingUid, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;

        var info = await LoadBookingAsync(bookingUid, cancellationToken);
        if (info == null) return;

        var (t, body) = NotificationTemplates.JobStarted(info.ProviderName, info.ServiceTitle);
        await NotifyClientAsync(info.ClientUid, NotificationTypes.JobStarted, t, body,
            NotificationScreens.RequestDetails, bookingUid, info.RequestUid, cancellationToken);
    }

    public async Task JobCompletedAsync(int bookingUid, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return;

        var info = await LoadBookingAsync(bookingUid, cancellationToken);
        if (info == null) return;

        var (ct, cbody) = NotificationTemplates.JobCompletedForClient(info.ServiceTitle);
        await NotifyClientAsync(info.ClientUid, NotificationTypes.JobCompleted, ct, cbody,
            NotificationScreens.RequestDetails, bookingUid, info.RequestUid, cancellationToken);

        var (pt, pbody) = NotificationTemplates.JobCompletedForProvider(info.ServiceTitle);
        await NotifyProviderAsync(info.ProviderUid, NotificationTypes.JobCompleted, pt, pbody,
            NotificationScreens.MyBookings, bookingUid, info.RequestUid, cancellationToken);
    }

    // ---- helpers ------------------------------------------------------------------------------

    private sealed record BookingInfo(
        int RequestUid, int ClientUid, int ProviderUid, string ServiceTitle, string? ProviderName, string? CancelReason);

    private Task<BookingInfo?> LoadBookingAsync(int bookingUid, CancellationToken cancellationToken) =>
        _db.ServiceBookings.AsNoTracking()
            .Where(b => b.Uid == bookingUid)
            .Select(b => new BookingInfo(
                b.RequestUid,
                b.ClientUid,
                b.ProviderUid,
                b.Request.ServiceTitle,
                b.Provider.FullName,
                b.CancelReason))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string> ServiceTitleAsync(int requestUid, CancellationToken cancellationToken) =>
        await _db.CustomerServiceRequests.AsNoTracking()
            .Where(r => r.Uid == requestUid)
            .Select(r => r.ServiceTitle)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

    private async Task NotifyClientAsync(int clientUid, string type, string title, string body, string screen,
        int? bookingUid, int? requestUid, CancellationToken cancellationToken)
    {
        var userId = await _db.Clients.AsNoTracking()
            .Where(c => c.Uid == clientUid)
            .Select(c => (int?)c.UserUid)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId == null) return;

        await _notifications.NotifyUserAsync(userId.Value, UserTypeConstants.Client, type, title, body, screen,
            bookingUid, requestUid, cancellationToken);
    }

    private async Task NotifyProviderAsync(int providerUid, string type, string title, string body, string screen,
        int? bookingUid, int? requestUid, CancellationToken cancellationToken)
    {
        var userId = await _db.Providers.AsNoTracking()
            .Where(p => p.Uid == providerUid)
            .Select(p => (int?)p.UserUid)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId == null) return;

        await _notifications.NotifyUserAsync(userId.Value, UserTypeConstants.Provider, type, title, body, screen,
            bookingUid, requestUid, cancellationToken);
    }
}

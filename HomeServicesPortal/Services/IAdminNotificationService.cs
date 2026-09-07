using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IAdminNotificationService
{
    Task NotifyServiceRequestCreatedAsync(
        int requestUid,
        string serviceTitle,
        string? clientName,
        CancellationToken cancellationToken = default);

    Task<AdminNotificationFeedDto> GetRecentAsync(
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default);
}

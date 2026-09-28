using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IAdminNotificationService
{
    Task NotifyServiceRequestCreatedAsync(
        int requestUid,
        string serviceTitle,
        string? clientName,
        CancellationToken cancellationToken = default);

    Task NotifyCustomerCancellationAsync(
        int requestUid,
        string serviceTitle,
        string? clientName,
        string? cancelReason,
        CancellationToken cancellationToken = default);

    Task NotifyProviderCancellationAsync(
        int bookingUid,
        string? providerName,
        string? cancelReason,
        CancellationToken cancellationToken = default);

    Task<AdminNotificationFeedDto> GetRecentAsync(
        int take = 20,
        IEnumerable<string>? types = null,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(
        IEnumerable<string>? types = null,
        CancellationToken cancellationToken = default);
}

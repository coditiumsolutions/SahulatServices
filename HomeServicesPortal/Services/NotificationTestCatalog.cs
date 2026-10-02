using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Services;

/// <summary>
/// The booking events the staff test page can fire. Wording comes from <see cref="NotificationTemplates"/>, so a test
/// shows exactly what the real flow would send. Role is the audience the real flow sends it to ("" = either).
/// </summary>
public static class NotificationTestCatalog
{
    public const string CustomKey = "custom";

    public sealed record Entry(
        string Key, string Label, string Role, string Type, string Screen,
        Func<string, string, string, (string Title, string Body)> Build);

    public static readonly IReadOnlyList<Entry> Entries = new[]
    {
        new Entry("job_assigned", "New job request (provider)", UserTypeConstants.Provider,
            NotificationTypes.JobAssigned, NotificationScreens.ProviderJobRequests,
            (service, _, _) => NotificationTemplates.JobAssigned(service)),
        new Entry("booking_accepted", "Provider accepted (client)", UserTypeConstants.Client,
            NotificationTypes.BookingAccepted, NotificationScreens.RequestDetails,
            (service, provider, _) => NotificationTemplates.BookingAccepted(provider, service)),
        new Entry("job_unavailable", "Job taken by another provider (provider)", UserTypeConstants.Provider,
            NotificationTypes.JobUnavailable, NotificationScreens.ProviderJobRequests,
            (service, _, _) => NotificationTemplates.JobUnavailable(service)),
        new Entry("provider_reassigning", "Finding another provider (client)", UserTypeConstants.Client,
            NotificationTypes.ProviderReassigning, NotificationScreens.RequestDetails,
            (service, _, _) => NotificationTemplates.ProviderReassigning(service)),
        new Entry("booking_cancelled_client", "Booking cancelled (client)", UserTypeConstants.Client,
            NotificationTypes.BookingCancelled, NotificationScreens.RequestDetails,
            (service, _, reason) => NotificationTemplates.BookingCancelledForClient(service, reason)),
        new Entry("booking_cancelled_provider", "Booking cancelled (provider)", UserTypeConstants.Provider,
            NotificationTypes.BookingCancelled, NotificationScreens.MyBookings,
            (service, _, reason) => NotificationTemplates.BookingCancelledForProvider(service, reason)),
        new Entry("job_started", "Job started (client)", UserTypeConstants.Client,
            NotificationTypes.JobStarted, NotificationScreens.RequestDetails,
            (service, provider, _) => NotificationTemplates.JobStarted(provider, service)),
        new Entry("job_completed_client", "Job completed (client)", UserTypeConstants.Client,
            NotificationTypes.JobCompleted, NotificationScreens.RequestDetails,
            (service, _, _) => NotificationTemplates.JobCompletedForClient(service)),
        new Entry("job_completed_provider", "Job completed (provider)", UserTypeConstants.Provider,
            NotificationTypes.JobCompleted, NotificationScreens.MyBookings,
            (service, _, _) => NotificationTemplates.JobCompletedForProvider(service)),
        new Entry("app_update", "App update available (either role)", string.Empty,
            NotificationTypes.AppUpdate, NotificationScreens.AppUpdate,
            (_, _, _) => ("Update available",
                "A new version of Sahulat Ghar Tak is available. Update now for the latest features and fixes."))
    };

    public static Entry? Find(string? key) =>
        Entries.FirstOrDefault(e => e.Key.Equals(key?.Trim(), StringComparison.OrdinalIgnoreCase));
}

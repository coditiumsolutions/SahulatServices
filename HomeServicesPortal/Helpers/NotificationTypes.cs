namespace HomeServicesPortal.Helpers;

/// <summary>Values of the push/inbox "type" field. Part of the app contract — see api.txt.</summary>
public static class NotificationTypes
{
    public const string JobAssigned = "job_assigned";
    public const string BookingAccepted = "booking_accepted";
    public const string JobUnavailable = "job_unavailable";
    public const string ProviderReassigning = "provider_reassigning";
    public const string BookingCancelled = "booking_cancelled";
    public const string JobStarted = "job_started";
    public const string JobCompleted = "job_completed";
    public const string AppUpdate = "app_update";

    /// <summary>Silent data-only push that lifts a forced-update block on the device (admin release). No inbox row.</summary>
    public const string AppUnblock = "app_unblock";
}

/// <summary>Values of the push/inbox "screen" field: route hints the Flutter app maps to real routes.</summary>
public static class NotificationScreens
{
    public const string RequestDetails = "request_details";
    public const string ProviderJobRequests = "provider_job_requests";
    public const string MyBookings = "my_bookings";
    public const string AppUpdate = "app_update";
}

/// <summary>
/// Android notification channel ids. The app creates these channels (sound and importance are channel settings on
/// Android 8+, so the backend can only say which channel a push belongs to). Part of the app contract, see api.txt. The _v2 ids exist because an Android channel's sound is fixed once created, so the sound-carrying channels got new ids.
/// </summary>
public static class NotificationChannels
{
    public const string JobRequests = "job_requests_v2";
    public const string BookingUpdates = "booking_updates_v2";
    public const string Announcements = "announcements_v2";

    public static string For(string? type) => type switch
    {
        NotificationTypes.JobAssigned => JobRequests,
        NotificationTypes.AppUpdate => Announcements,
        _ => BookingUpdates
    };
}

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
}

/// <summary>Values of the push/inbox "screen" field: route hints the Flutter app maps to real routes.</summary>
public static class NotificationScreens
{
    public const string RequestDetails = "request_details";
    public const string ProviderJobRequests = "provider_job_requests";
    public const string MyBookings = "my_bookings";
    public const string AppUpdate = "app_update";
}

namespace HomeServicesPortal.Options;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>
    /// Kill switch for booking-lifecycle notifications (push AND inbox rows). Read through IOptionsMonitor,
    /// so flipping it in appsettings.Production.json takes effect without a restart. Does not affect the
    /// admin broadcast or the app-config version check.
    /// </summary>
    public bool BookingPushEnabled { get; set; } = true;

    /// <summary>
    /// When true, Android pushes name a notification channel (job_requests_v2 / booking_updates_v2 / announcements_v2).
    /// Keep false until an app build that creates those channels is live: Android sends a push naming an
    /// unknown channel to the generic fallback channel (no heads-up banner), which would downgrade the
    /// currently published builds. Read through IOptionsMonitor, so it flips without a restart.
    /// </summary>
    public bool AndroidChannelsEnabled { get; set; } = false;
}

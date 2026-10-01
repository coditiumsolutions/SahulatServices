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
}

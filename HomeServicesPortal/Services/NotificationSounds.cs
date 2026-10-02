using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Services;

/// <summary>
/// Custom sound per notification channel. Every entry is null until the sound files are chosen and shipped in the
/// app, so pushes currently use the device default. To add a sound see docs/notification-sounds.md: put the files in
/// the app, then set the names below (no code change anywhere else).
/// </summary>
public static class NotificationSounds
{
    /// <summary>
    /// Android raw resource name without extension (res/raw/job_request.ogg is "job_request"). Only honoured on
    /// Android 7 and older; on 8+ the sound belongs to the channel the app created.
    /// </summary>
    public static string? Android(string channelId) => channelId switch
    {
        NotificationChannels.JobRequests => null,     // e.g. "job_request"
        NotificationChannels.BookingUpdates => null,  // e.g. "booking_update"
        NotificationChannels.Announcements => null,   // e.g. "announcement"
        _ => null
    };

    /// <summary>iOS file name inside the app bundle, with extension (.caf / .aiff / .wav, 30 seconds or less).</summary>
    public static string? Ios(string channelId) => channelId switch
    {
        NotificationChannels.JobRequests => null,     // e.g. "job_request.caf"
        NotificationChannels.BookingUpdates => null,  // e.g. "booking_update.caf"
        NotificationChannels.Announcements => null,   // e.g. "announcement.caf"
        _ => null
    };
}

using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Services;

/// <summary>
/// Custom sound per notification channel. The files live in the Flutter app (ogg for Android, wav for iOS) and must
/// be shipped in the app build under the names below. See docs/notification-sounds.md. A build that lacks a
/// file just plays the device default.
/// </summary>
public static class NotificationSounds
{
    /// <summary>
    /// Android raw resource name without extension (res/raw/job_request.ogg is "job_request"). Only honoured on
    /// Android 7 and older; on 8+ the sound belongs to the channel the app created.
    /// </summary>
    public static string? Android(string channelId) => channelId switch
    {
        NotificationChannels.JobRequests => "job_request",
        NotificationChannels.BookingUpdates => "booking_update",
        NotificationChannels.Announcements => "announcement",
        _ => null
    };

    /// <summary>iOS file name inside the app bundle, with extension (.caf / .aiff / .wav, 30 seconds or less).</summary>
    public static string? Ios(string channelId) => channelId switch
    {
        NotificationChannels.JobRequests => "job_request.wav",
        NotificationChannels.BookingUpdates => "booking_update.wav",
        NotificationChannels.Announcements => "announcement.wav",
        _ => null
    };
}

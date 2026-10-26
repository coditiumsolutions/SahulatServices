using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Helpers;

/// <summary>Icon, colour and web target for an inbox notification (mirrors the app's notification screen and router).</summary>
public static class NotificationDisplayHelper
{
    public static (string Icon, string Colour) IconFor(string? type) => type switch
    {
        "job_assigned" => ("fa-solid fa-briefcase", "#016EE3"),
        "booking_accepted" => ("fa-regular fa-circle-check", "#16A34A"),
        "job_completed" => ("fa-solid fa-circle-check", "#16A34A"),
        "job_started" => ("fa-regular fa-circle-play", "#0D9488"),
        "provider_reassigning" => ("fa-solid fa-rotate", "#D97706"),
        "booking_cancelled" => ("fa-regular fa-circle-xmark", "#DC2626"),
        "job_unavailable" => ("fa-solid fa-ban", "#DC2626"),
        "app_update" => ("fa-solid fa-mobile-screen", "#7C3AED"),
        _ => ("fa-regular fa-bell", "#016EE3")
    };

    /// <summary>Where tapping a notification goes on the website, or null to stay on the inbox.</summary>
    public static string? TargetFor(UserNotificationApiDto n, string role)
    {
        var client = role == UserTypeConstants.Client;
        switch (n.Screen)
        {
            case "request_details" when client:
                return n.RequestUid is > 0 ? $"/client/requests/{n.RequestUid}" : "/client/requests";
            case "provider_job_requests" when !client:
                return "/provider/requests";
            case "my_bookings" when !client:
                return n.BookingUid is > 0 ? $"/provider/bookings/{n.BookingUid}" : "/provider/bookings";
            default:
                return null; // app_update and anything unknown: nothing to open on the web
        }
    }

    /// <summary>Only same-site portal paths may be used as a redirect target.</summary>
    public static bool IsSafeTarget(string? next) =>
        !string.IsNullOrEmpty(next)
        && System.Text.RegularExpressions.Regex.IsMatch(next, @"^/(client|provider)/[A-Za-z0-9/_-]*$");
}

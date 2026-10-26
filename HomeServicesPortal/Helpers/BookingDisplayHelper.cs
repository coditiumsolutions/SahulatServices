using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Helpers;

/// <summary>Display helpers for the provider booking pages (statuses, colours and steps match the app).</summary>
public static class BookingDisplayHelper
{
    /// <summary>Reasons a provider can pick when rejecting an offer or cancelling a booking.</summary>
    public static readonly string[] ProviderReasons =
    {
        "Outside my service area",
        "Fully booked / no availability",
        "Job scope unclear",
        "Price too low",
        "Other"
    };

    public static readonly string[] ProgressSteps = { "Requested", "Accepted", "In Progress", "Completed" };

    /// <summary>Position on the 4-step bar, or -1 for Cancelled/Rejected (shown as a red label).</summary>
    public static int Step(string? status) => status switch
    {
        "Pending" => 0,
        "Accepted" => 1,
        "In Progress" => 2,
        "Completed" or "Closed" => 3,
        _ => -1
    };

    /// <summary>Bootstrap colour name for the status badge.</summary>
    public static string Colour(string? status) => status switch
    {
        "Pending" => "secondary",
        "Accepted" => "warning",
        "In Progress" => "info",
        "Completed" or "Closed" => "success",
        "Cancelled" or "Rejected" => "danger",
        _ => "secondary"
    };

    public static string Address(ServiceBookingApiDto b) =>
        string.Join(", ", new[] { b.ClientFullAddress, b.ClientArea, b.ClientCity }
            .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

    public static string? Scheduled(ServiceBookingApiDto b) =>
        b.PreferredServiceDate == null && string.IsNullOrWhiteSpace(b.PreferredServiceTime)
            ? null
            : RequestDisplayHelper.ScheduleText(b.PreferredServiceDate, b.PreferredServiceTime);

    /// <summary>"Rs 1,500" including zero, for the payment card where 0 is a real value.</summary>
    public static string Money(decimal amount) =>
        "Rs " + Math.Round(amount, MidpointRounding.AwayFromZero).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    public static string PaymentModeLabel(string? mode) => mode switch
    {
        "CashToProvider" => "Cash to Provider",
        "OnlineToCompany" => "Online to Company",
        _ => string.IsNullOrWhiteSpace(mode) ? "-" : mode
    };

    /// <summary>Wallet row label: known reasons by name, anything else split from PascalCase into words.</summary>
    public static string WalletReasonLabel(string? reason)
    {
        switch (reason)
        {
            case "JobEarning": return "Job Earning";
            case "CashCollect": return "Cash Collected";
            case "Payout": return "Payout";
            case null:
            case "": return "-";
        }

        return System.Text.RegularExpressions.Regex.Replace(reason, "(?<=[a-z0-9])(?=[A-Z])", " ");
    }

    /// <summary>Google Maps directions link, same as the app's Directions button.</summary>
    public static string? DirectionsUrl(ServiceBookingApiDto b) =>
        b.ClientLatitude is { } lat && b.ClientLongitude is { } lng
            ? FormattableString.Invariant($"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}&travelmode=driving")
            : null;

    /// <summary>OpenStreetMap embed centred on the client's pin, with a marker.</summary>
    public static string? EmbedMapUrl(ServiceBookingApiDto b)
    {
        if (b.ClientLatitude is not { } lat || b.ClientLongitude is not { } lng)
        {
            return null;
        }

        const double dLon = 0.006, dLat = 0.004;
        return FormattableString.Invariant(
            $"https://www.openstreetmap.org/export/embed.html?bbox={(double)lng - dLon},{(double)lat - dLat},{(double)lng + dLon},{(double)lat + dLat}&layer=mapnik&marker={lat},{lng}");
    }
}

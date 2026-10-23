using System.Globalization;

namespace HomeServicesPortal.Helpers;

/// <summary>Display helpers shared by the client request pages (formats match the mobile app).</summary>
public static class RequestDisplayHelper
{
    /// <summary>"dd-MM-yyyy · hh:mm tt" from the wall-clock date and time (no timezone conversion), or "Not specified".</summary>
    public static string ScheduleText(DateOnly? date, string? time)
    {
        if (date == null && string.IsNullOrWhiteSpace(time))
        {
            return "Not specified";
        }

        var parts = new List<string>();
        if (date != null)
        {
            parts.Add(date.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
        }

        var timeText = TimeText(time);
        if (timeText != null)
        {
            parts.Add(timeText);
        }

        return string.Join(" · ", parts);
    }

    public static string? TimeText(string? time)
    {
        if (string.IsNullOrWhiteSpace(time) || !TimeOnly.TryParse(time, CultureInfo.InvariantCulture, out var parsed))
        {
            return null;
        }

        return parsed.ToString(TimeFormatPreference.Use12Hour ? "hh:mm tt" : "HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>"Rs 1,500" with no decimals; null when the amount is missing or not positive.</summary>
    public static string? Currency(decimal? amount) =>
        amount is > 0 ? "Rs " + Math.Round(amount.Value, MidpointRounding.AwayFromZero).ToString("N0", CultureInfo.InvariantCulture) : null;

    /// <summary>wa.me link: 0XXXXXXXXXX becomes 92XXXXXXXXXX, numbers starting with 92 stay as they are.</summary>
    public static string WhatsAppUrl(string? mobile)
    {
        var digits = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith('0'))
        {
            digits = "92" + digits[1..];
        }

        return "https://wa.me/" + digits;
    }

    /// <summary>Formats 03XXXXXXXXX as 03XX-XXXXXXX.</summary>
    public static string MobileDisplay(string? mobile)
    {
        var digits = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 11 ? digits[..4] + "-" + digits[4..] : mobile ?? string.Empty;
    }
}

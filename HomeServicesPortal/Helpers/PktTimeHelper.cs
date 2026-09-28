namespace HomeServicesPortal.Helpers;

public static class PktTimeHelper
{
    private static readonly TimeSpan PktOffset = TimeSpan.FromHours(5); // PKT = UTC+5, no DST

    /// <summary>Converts a UTC DateTime to Pakistan Standard Time. Assumes input is UTC regardless of Kind.</summary>
    public static DateTime ToPkt(this DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(PktOffset);

    public static DateTime? ToPkt(this DateTime? utc) => utc.HasValue ? utc.Value.ToPkt() : null;

    /// <summary>Formats a UTC DateTime as PKT using the given .NET format string (e.g. "dd MMM yyyy HH:mm").</summary>
    public static string ToPktString(this DateTime utc, string format) => utc.ToPkt().ToString(format);

    public static string ToPktString(this DateTime? utc, string format, string fallback = "-") =>
        utc.HasValue ? utc.Value.ToPktString(format) : fallback;

    /// <summary>
    /// Standardized admin-portal date+time display: date as "dd-MM-yyyy" (repo-wide convention),
    /// time as "HH:mm" or "hh:mm tt" depending on the admin's saved Preferences > Time Format
    /// toggle (TimeFormatPreference). Use this instead of a hardcoded ToPktString format string
    /// for any new date+time display so it stays in sync with the preference automatically.
    /// </summary>
    public static string ToPktDisplay(this DateTime utc) =>
        utc.ToPktString(TimeFormatPreference.Use12Hour ? "dd-MM-yyyy hh:mm tt" : "dd-MM-yyyy HH:mm");

    public static string ToPktDisplay(this DateTime? utc, string fallback = "-") =>
        utc.HasValue ? utc.Value.ToPktDisplay() : fallback;

    /// <summary>Date-only admin-portal display: "dd-MM-yyyy" (no time component).</summary>
    public static string ToPktDateDisplay(this DateTime utc) => utc.ToPktString("dd-MM-yyyy");

    public static string ToPktDateDisplay(this DateTime? utc, string fallback = "-") =>
        utc.HasValue ? utc.Value.ToPktDateDisplay() : fallback;

    /// <summary>Compact date-only admin-portal display for tight-space columns: "dd-MM-yy".</summary>
    public static string ToPktShortDateDisplay(this DateTime utc) => utc.ToPktString("dd-MM-yy");

    public static string ToPktShortDateDisplay(this DateTime? utc, string fallback = "-") =>
        utc.HasValue ? utc.Value.ToPktShortDateDisplay() : fallback;

    /// <summary>Time-only admin-portal display, honoring the 12h/24h Preferences toggle.</summary>
    public static string ToPktTimeDisplay(this DateTime utc) =>
        utc.ToPktString(TimeFormatPreference.Use12Hour ? "hh:mm tt" : "HH:mm");

    public static string ToPktTimeDisplay(this DateTime? utc, string fallback = "-") =>
        utc.HasValue ? utc.Value.ToPktTimeDisplay() : fallback;

    /// <summary>
    /// Wall-clock TimeOnly display (e.g. ProviderAvailability.AvailableFrom/To — already local,
    /// no PKT conversion needed), honoring the 12h/24h Preferences toggle.
    /// </summary>
    public static string ToDisplay(this TimeOnly time) =>
        time.ToString(TimeFormatPreference.Use12Hour ? "hh:mm tt" : "HH:mm");

    public static string ToDisplay(this TimeOnly? time, string fallback = "-") =>
        time.HasValue ? time.Value.ToDisplay() : fallback;

    /// <summary>Wall-clock DateOnly display: "dd-MM-yyyy" (repo-wide standard).</summary>
    public static string ToDisplay(this DateOnly date) => date.ToString("dd-MM-yyyy");

    public static string ToDisplay(this DateOnly? date, string fallback = "-") =>
        date.HasValue ? date.Value.ToDisplay() : fallback;

    /// <summary>Compact DateOnly display for tight-space columns: "dd-MM-yy".</summary>
    public static string ToShortDisplay(this DateOnly date) => date.ToString("dd-MM-yy");

    public static string ToShortDisplay(this DateOnly? date, string fallback = "-") =>
        date.HasValue ? date.Value.ToShortDisplay() : fallback;

    /// <summary>
    /// Reformats a free-text "HH:mm"-style time string (e.g. CustomerServiceRequests.PreferredServiceTime)
    /// for display, honoring the 12h/24h Preferences toggle. Falls back to the raw stored value
    /// unchanged if it isn't a parseable time (never rejects/hides data over a display quirk).
    /// </summary>
    public static string ToTimeDisplay(this string? rawTime, string fallback = "-")
    {
        if (string.IsNullOrWhiteSpace(rawTime)) return fallback;
        return TimeOnly.TryParse(rawTime, out var time) ? time.ToDisplay() : rawTime;
    }
}

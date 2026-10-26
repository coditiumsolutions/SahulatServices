namespace HomeServicesPortal.Helpers;

/// <summary>
/// Process-wide cache of the admin portal's 12h/24h time display preference. Backed by the
/// Configurations table (key "TimeFormat", value "12h" or "24h") via ConfigurationEntryService,
/// but cached in-memory so every view/helper can read it synchronously without an async DB call
/// per render. Refreshed immediately whenever PreferencesController saves a new value.
/// Single-process deployment (one systemd service instance) — no cross-instance cache invalidation needed.
/// </summary>
public static class TimeFormatPreference
{
    public const string ConfigKey = "TimeFormat";
    public const string TwelveHour = "12h";
    public const string TwentyFourHour = "24h";

    /// <summary>Cookie holding one visitor's own choice ("12h" or "24h"). It overrides the admin default for them only.</summary>
    public const string UserCookieName = "sgt_time_format";

    private static volatile bool _use12Hour;

    // Set by UserTimeFormatMiddleware for the duration of one request; null = use the admin default.
    private static readonly AsyncLocal<bool?> _userUse12Hour = new();

    /// <summary>The signed-in visitor's own choice if they made one, otherwise the admin portal's default.</summary>
    public static bool Use12Hour => _userUse12Hour.Value ?? _use12Hour;

    /// <summary>Applies one request's per-visitor override (null clears it).</summary>
    public static void SetForRequest(string? cookieValue) =>
        _userUse12Hour.Value = cookieValue switch
        {
            TwelveHour => true,
            TwentyFourHour => false,
            _ => null
        };

    public static void Set(string? configValue) => _use12Hour = configValue == TwelveHour;
}

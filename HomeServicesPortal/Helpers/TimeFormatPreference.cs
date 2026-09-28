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

    private static volatile bool _use12Hour;

    public static bool Use12Hour => _use12Hour;

    public static void Set(string? configValue) => _use12Hour = configValue == TwelveHour;
}

namespace HomeServicesPortal.Helpers;

/// <summary>
/// In-app inbox limits, editable by staff under Admin > Configurations (rows in dbo.Configurations).
/// A missing or invalid row falls back to the default, so deleting a row never breaks the inbox.
/// </summary>
public static class InboxRetention
{
    public const string DaysKey = "Inbox.RetentionDays";
    public const string MaxPerRoleKey = "Inbox.MaxPerRole";

    public const int DefaultDays = 90;
    public const int DefaultMaxPerRole = 200;

    public const int MinDays = 7, MaxDays = 365;
    public const int MinMaxPerRole = 20, MaxMaxPerRole = 1000;

    public static bool IsInboxKey(string? key) =>
        string.Equals(key, DaysKey, StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, MaxPerRoleKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>Validation message for an inbox limit value, or null when it is fine (or the key isn't an inbox key).</summary>
    public static string? Validate(string? key, string? value)
    {
        var (min, max) = string.Equals(key, DaysKey, StringComparison.OrdinalIgnoreCase)
            ? (MinDays, MaxDays)
            : string.Equals(key, MaxPerRoleKey, StringComparison.OrdinalIgnoreCase)
                ? (MinMaxPerRole, MaxMaxPerRole)
                : (0, 0);
        if (max == 0) return null;

        return int.TryParse(value?.Trim(), out var n) && n >= min && n <= max
            ? null
            : $"'{key!.Trim()}' must be a whole number between {min} and {max}.";
    }

    public static int ParseOrDefault(string? value, int fallback, int min, int max) =>
        int.TryParse(value?.Trim(), out var n) && n >= min && n <= max ? n : fallback;
}

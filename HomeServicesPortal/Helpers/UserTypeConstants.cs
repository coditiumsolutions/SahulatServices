namespace HomeServicesPortal.Helpers;

public static class UserTypeConstants
{
    public const string Client = "Client";
    public const string Provider = "Provider";
    public const string Staff = "Staff";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Client,
        Provider,
        Staff
    };

    public static bool IsValid(string? userType) =>
        !string.IsNullOrWhiteSpace(userType) && Allowed.Contains(userType);
}

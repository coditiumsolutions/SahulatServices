namespace HomeServicesPortal.Helpers;

/// <summary>
/// Admin portal roles mapped onto UsersLogin.UserType (+ Staff.IsAdmin).
/// Combines former Administration/Roles into the Users form.
/// </summary>
public static class PortalRoleConstants
{
    public const string Client = "Client";
    public const string Provider = "Provider";
    public const string Staff = "Staff";
    public const string SuperAdmin = "Super Admin";

    public static readonly IReadOnlyList<string> All =
    [
        Client,
        Provider,
        Staff,
        SuperAdmin
    ];

    public static bool IsValid(string? role) =>
        !string.IsNullOrWhiteSpace(role) &&
        All.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

    public static (string UserType, bool IsAdmin) ToUserType(string role)
    {
        if (role.Equals(SuperAdmin, StringComparison.OrdinalIgnoreCase))
            return (UserTypeConstants.Staff, true);

        if (role.Equals(Staff, StringComparison.OrdinalIgnoreCase))
            return (UserTypeConstants.Staff, false);

        if (role.Equals(Provider, StringComparison.OrdinalIgnoreCase))
            return (UserTypeConstants.Provider, false);

        return (UserTypeConstants.Client, false);
    }

    public static string FromUser(string userType, bool isAdmin)
    {
        if (userType.Equals(UserTypeConstants.Staff, StringComparison.OrdinalIgnoreCase))
            return isAdmin ? SuperAdmin : Staff;

        if (userType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
            return Provider;

        return Client;
    }
}

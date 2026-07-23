namespace HomeServicesPortal.Helpers;

public static class OtpTypeConstants
{
    public const string Registration = "Registration";
    public const string Login = "Login";
    public const string PasswordReset = "PasswordReset";

    public static readonly string[] All = [Registration, Login, PasswordReset];

    public static bool IsValid(string? otpType) =>
        !string.IsNullOrWhiteSpace(otpType)
        && All.Any(t => t.Equals(otpType.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string otpType) =>
        All.First(t => t.Equals(otpType.Trim(), StringComparison.OrdinalIgnoreCase));
}

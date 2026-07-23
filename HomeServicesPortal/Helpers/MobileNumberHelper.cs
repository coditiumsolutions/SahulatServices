using System.Text.RegularExpressions;

namespace HomeServicesPortal.Helpers;

public static partial class MobileNumberHelper
{
    /// <summary>
    /// Normalizes and validates Pakistani-style mobile numbers (e.g. 03XXXXXXXXX).
    /// Accepts optional country code prefix +92.
    /// </summary>
    public static (bool IsValid, string Normalized, string? Error) ValidateAndNormalize(string? mobileNo)
    {
        if (string.IsNullOrWhiteSpace(mobileNo))
        {
            return (false, string.Empty, "Mobile number is required.");
        }

        var digits = DigitsOnlyRegex().Replace(mobileNo.Trim(), string.Empty);

        if (digits.StartsWith("92", StringComparison.Ordinal) && digits.Length >= 12)
        {
            digits = "0" + digits[2..];
        }

        if (!PakistaniMobileRegex().IsMatch(digits))
        {
            return (false, string.Empty, "Invalid mobile number format. Use 03XXXXXXXXX.");
        }

        return (true, digits, null);
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnlyRegex();

    [GeneratedRegex(@"^03\d{9}$")]
    private static partial Regex PakistaniMobileRegex();
}

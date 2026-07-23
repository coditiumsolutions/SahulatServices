using System.ComponentModel.DataAnnotations;
using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.DTOs;

public class SendOtpRequest : IValidatableObject
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10)]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP type is required.")]
    [StringLength(20)]
    public string OTPType { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!OtpTypeConstants.IsValid(OTPType))
        {
            yield return new ValidationResult(
                "OTP type must be Registration, Login, or PasswordReset.",
                [nameof(OTPType)]);
        }
    }
}

public class ResendOtpRequest : IValidatableObject
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10)]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP type is required.")]
    [StringLength(20)]
    public string OTPType { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!OtpTypeConstants.IsValid(OTPType))
        {
            yield return new ValidationResult(
                "OTP type must be Registration, Login, or PasswordReset.",
                [nameof(OTPType)]);
        }
    }
}

public class VerifyOtpRequest
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10)]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required.")]
    [StringLength(10, MinimumLength = 4)]
    public string OTP { get; set; } = string.Empty;
}

public class SendOtpResponse
{
    public string MobileNo { get; set; } = string.Empty;

    public string OTPType { get; set; } = string.Empty;

    public DateTime ExpiryTime { get; set; }

    /// <summary>Populated only in Development. Always null in Production.</summary>
    public string? OTP { get; set; }
}

public class VerifyOtpResponse
{
    public string MobileNo { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public bool UserVerified { get; set; }
}

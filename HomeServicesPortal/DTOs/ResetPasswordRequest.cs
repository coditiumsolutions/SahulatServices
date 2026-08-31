using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.DTOs;

public class ResetPasswordRequest : IValidatableObject
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Mobile number must be between 10 and 20 characters.")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required.")]
    [StringLength(10, MinimumLength = 4)]
    public string OTP { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm new password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "New password and confirm password do not match.",
                [nameof(ConfirmNewPassword)]);
        }
    }
}

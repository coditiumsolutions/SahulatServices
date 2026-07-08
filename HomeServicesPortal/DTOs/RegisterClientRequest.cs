using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.DTOs;

public class RegisterClientRequest : IValidatableObject
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Mobile number must be between 10 and 20 characters.")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Confirm password must match password.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(15, ErrorMessage = "CNIC cannot exceed 15 characters.")]
    public string? CNIC { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Confirm password must match password.",
                [nameof(ConfirmPassword)]);
        }
    }
}

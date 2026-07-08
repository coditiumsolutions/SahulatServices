using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.DTOs;

public class RegisterProviderRequest : IValidatableObject
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Mobile number must be between 10 and 20 characters.")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "CNIC is required.")]
    [StringLength(15, ErrorMessage = "CNIC cannot exceed 15 characters.")]
    public string CNIC { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Gender { get; set; }

    [Range(0, 60, ErrorMessage = "Experience years must be between 0 and 60.")]
    public int? ExperienceYears { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Category id must be greater than 0.")]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    public string? CategoryName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CategoryId.HasValue && string.IsNullOrWhiteSpace(CategoryName))
        {
            yield return new ValidationResult(
                "CategoryId or CategoryName is required.",
                [nameof(CategoryId), nameof(CategoryName)]);
        }
    }
}

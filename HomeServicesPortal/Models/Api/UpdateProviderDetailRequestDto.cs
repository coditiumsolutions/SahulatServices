using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class UpdateProviderDetailRequestDto : IValidatableObject
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProviderUid { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CNIC is required.")]
    [StringLength(15)]
    public string CNIC { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Gender { get; set; }

    [Range(0, 60)]
    public int? ExperienceYears { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
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

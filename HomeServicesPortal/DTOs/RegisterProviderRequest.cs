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

    /// <summary>One of the configured city options (see GET /api/cities). Optional — falls back to the client's existing city if omitted.</summary>
    [StringLength(100)]
    public string? City { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Category id must be greater than 0.")]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    public string? CategoryName { get; set; }

    // --- Added for provider multi-category support (2026-09-21) ---
    // OPTIONAL and additive: an app build that omits CategoryIds keeps today's exact behavior
    // (single category, from CategoryId/CategoryName above, becomes the provider's primary and
    // only category). Only when CategoryIds is sent does registration seed multiple
    // ProviderCategories rows in one step.
    // TODO(remove after old app retired): once every live app build always sends CategoryIds
    // for provider registration, CategoryId/CategoryName above can be retired in favor of always
    // requiring CategoryIds + PrimaryCategoryId, and this optional-list branch in
    // AuthService.RegisterProviderAsync can be simplified to the single required path.

    /// <summary>Optional multi-category selection. If provided, must include PrimaryCategoryId. If omitted, CategoryId/CategoryName (single category) is used as before.</summary>
    public List<int>? CategoryIds { get; set; }

    /// <summary>Required when CategoryIds is provided; must be one of the values in CategoryIds.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Primary category id must be greater than 0.")]
    public int? PrimaryCategoryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryIds is { Count: > 0 })
        {
            if (!PrimaryCategoryId.HasValue)
            {
                yield return new ValidationResult(
                    "PrimaryCategoryId is required when CategoryIds is provided.",
                    [nameof(PrimaryCategoryId)]);
            }
            else if (!CategoryIds.Contains(PrimaryCategoryId.Value))
            {
                yield return new ValidationResult(
                    "PrimaryCategoryId must be one of the values in CategoryIds.",
                    [nameof(PrimaryCategoryId)]);
            }

            yield break;
        }

        if (!CategoryId.HasValue && string.IsNullOrWhiteSpace(CategoryName))
        {
            yield return new ValidationResult(
                "CategoryId or CategoryName is required.",
                [nameof(CategoryId), nameof(CategoryName)]);
        }
    }
}

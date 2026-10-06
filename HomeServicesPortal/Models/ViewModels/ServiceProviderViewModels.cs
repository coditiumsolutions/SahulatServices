using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ServiceProviderListVm
{
    public List<ServiceProviderItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    /// <summary>Filter: null/empty = all, "1" = Verified, "0" = Not Verified.</summary>
    public string? VerifyStatus { get; set; }
    public string Sort { get; set; } = "id";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ServiceProviderItemVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ExperienceYears { get; set; }
    public decimal? Rating { get; set; }
    public bool IsVerified { get; set; }
    public string? ProfilePicturePath { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceProviderFormVm : IValidatableObject
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20)]
    [RegularExpression(@"^03\d{9}$", ErrorMessage = "Enter a valid mobile number in the format 03XXXXXXXXX (11 digits).")]
    [Display(Name = "Mobile No")]
    public string? MobileNo { get; set; }

    [Required(ErrorMessage = "CNIC is required.")]
    [StringLength(15)]
    [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "Enter a valid CNIC in the format XXXXX-XXXXXXX-X.")]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [StringLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(100)]
    [Display(Name = "Zone")]
    public string? Zone { get; set; }

    /// <summary>Primary category (ProviderCategories.PrimaryCategory = 1). Must be one of CategoryUids.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Primary Category is required.")]
    [Display(Name = "Primary Category")]
    public int CategoryUid { get; set; }

    /// <summary>All categories this provider offers. Between 1 and 3, inclusive.</summary>
    [MinLength(1, ErrorMessage = "Select at least one category.")]
    [MaxLength(3, ErrorMessage = "Select at most 3 categories.")]
    [Display(Name = "Categories")]
    public List<int> CategoryUids { get; set; } = new();

    /// <summary>
    /// Optional. Predefined ServiceTitles this provider offers, within the categories they have
    /// (CategoryUids). Fully optional — unlike categories, an empty list is a normal state.
    /// </summary>
    [Display(Name = "Service Titles")]
    public List<int> ServiceTitleUids { get; set; } = new();

    public List<ServiceTitleOptionVm> ServiceTitleOptions { get; set; } = new();

    [Display(Name = "Experience Years")]
    [Range(0, 60)]
    public int? ExperienceYears { get; set; }

    [Display(Name = "Rating")]
    public decimal? Rating { get; set; }

    [Display(Name = "Is Verified")]
    public bool IsVerified { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePicture { get; set; }

    public string? ExistingProfilePicturePath { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> CityOptions { get; set; } = new();
    public List<SelectListItem> ZoneOptions { get; set; } = new();

    /// <summary>Embedded Legal Documents create/edit form (Edit page tab).</summary>
    public ProviderDocumentFormVm DocumentForm { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Uid != 0)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required.", [nameof(Password)]);
        }
        else if (Password.Length < 4)
        {
            yield return new ValidationResult("Password must be at least 4 characters.", [nameof(Password)]);
        }
    }
}

public class ServiceTitleOptionVm
{
    public int Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CategoryUid { get; set; }
}

public class ServiceProviderDetailsVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string? City { get; set; }
    public string? Zone { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ExperienceYears { get; set; }
    public decimal? Rating { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public string? ProfilePicturePath { get; set; }
    public DateTime? CreatedOn { get; set; }

    /// <summary>Legal documents for this provider, matched by MobileNo (1:1).</summary>
    public ServiceProviderDocumentTabVm? Documents { get; set; }
}

public class ServiceProviderDocumentTabVm
{
    public int Uid { get; set; }
    public string MobileNo { get; set; } = string.Empty;
    public string? ProfilePhotoPath { get; set; }
    public string? CnicFrontImagePath { get; set; }
    public string? CnicBackImagePath { get; set; }
    public string? PoliceVerificationPath { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public int? VerifiedBy { get; set; }
    public string? VerificationRemarks { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class ServiceProviderDeleteVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public int DocumentCount { get; set; }
    public int BookingCount { get; set; }
    public int PaymentLedgerCount { get; set; }
    public int PayoutCount { get; set; }
    public int CommissionRuleCount { get; set; }
    public bool HasLinkedData =>
        DocumentCount > 0
        || BookingCount > 0
        || PaymentLedgerCount > 0
        || PayoutCount > 0
        || CommissionRuleCount > 0;
}

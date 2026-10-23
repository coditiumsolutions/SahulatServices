using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

/// <summary>Public site signup for Client or Provider (UsersLogin + matching profile).</summary>
public class PublicSignupViewModel
{
    public const int MaxCategories = 3;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [Display(Name = "Mobile Number")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a gender.")]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Account type is required.")]
    [Display(Name = "I am a")]
    public string UserType { get; set; } = string.Empty;

    [Display(Name = "CNIC")]
    [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "Enter a valid 13-digit CNIC.")]
    public string? Cnic { get; set; }

    /// <summary>Provider only: 1 to <see cref="MaxCategories"/> categories.</summary>
    [Display(Name = "Categories")]
    public List<int> CategoryUids { get; set; } = new();

    /// <summary>Provider only: the primary category, one of <see cref="CategoryUids"/>.</summary>
    [Display(Name = "Primary Category")]
    public int? PrimaryCategoryUid { get; set; }

    [Display(Name = "City")]
    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "Experience (years)")]
    [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years.")]
    public int? ExperienceYears { get; set; }

    [Display(Name = "Description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Minimum 6 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Cities { get; set; } = Array.Empty<SelectListItem>();
}

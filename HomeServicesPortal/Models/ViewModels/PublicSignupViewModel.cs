using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

/// <summary>Public site signup for Client or Provider (UsersLogin + matching profile).</summary>
public class PublicSignupViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [Display(Name = "Mobile Number")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account type is required.")]
    [Display(Name = "I am a")]
    public string UserType { get; set; } = string.Empty;

    [Display(Name = "CNIC")]
    [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "Enter a valid CNIC in the format XXXXX-XXXXXXX-X.")]
    public string? Cnic { get; set; }

    [Display(Name = "Service Category")]
    public int? CategoryUid { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
}

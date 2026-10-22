using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

/// <summary>Public site login for Client and Provider accounts (not Staff/admin).</summary>
public class PublicLoginViewModel
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [Display(Name = "Mobile Number")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

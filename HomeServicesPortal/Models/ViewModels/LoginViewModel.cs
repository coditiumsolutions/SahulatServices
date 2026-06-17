using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

/// <summary>
/// View model for the account login form with client and server-side validation.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// Optional return URL after successful sign-in.
    /// </summary>
    public string? ReturnUrl { get; set; }
}

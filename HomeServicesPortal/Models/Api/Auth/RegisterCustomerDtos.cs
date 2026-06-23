using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api.Auth;

public class RegisterCustomerRequestDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DefaultAddress { get; set; }
}

public class RegisterCustomerResponseDto
{
    public int UserId { get; set; }

    public int ProfileId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Customer";
}

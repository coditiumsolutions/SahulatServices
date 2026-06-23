using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api.Auth;

public class RegisterProviderRequestDto
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

    /// <summary>Service category id from ServiceCategories table.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Service category name, e.g. Electrician, Plumber.</summary>
    [StringLength(100)]
    public string? ServiceType { get; set; }

    [StringLength(20)]
    public string? Cnic { get; set; }

    [Range(0, 60)]
    public int? ExperienceYears { get; set; }
}

public class RegisterProviderResponseDto
{
    public int UserId { get; set; }

    public int ProfileId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "Provider";

    public int? CategoryId { get; set; }

    public string? ServiceType { get; set; }
}

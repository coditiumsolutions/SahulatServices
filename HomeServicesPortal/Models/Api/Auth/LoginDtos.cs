using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email or phone is required.")]
    [Display(Name = "Email or Phone")]
    public string EmailOrPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}

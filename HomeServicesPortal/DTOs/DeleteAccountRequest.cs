using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.DTOs;

public class DeleteAccountRequest
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Mobile number must be between 10 and 20 characters.")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

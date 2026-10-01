using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class RegisterDeviceTokenRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "userId is required.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "userType is required.")]
    public string UserType { get; set; } = string.Empty;

    [Required(ErrorMessage = "deviceToken is required.")]
    [StringLength(512, ErrorMessage = "deviceToken is too long.")]
    public string DeviceToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "platform is required.")]
    public string Platform { get; set; } = string.Empty;
}

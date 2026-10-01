using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class UnregisterDeviceTokenRequestDto
{
    [Required(ErrorMessage = "deviceToken is required.")]
    [StringLength(512, ErrorMessage = "deviceToken is too long.")]
    public string DeviceToken { get; set; } = string.Empty;
}

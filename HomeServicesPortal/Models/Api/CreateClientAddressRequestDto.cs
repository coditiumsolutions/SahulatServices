using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class CreateClientAddressRequestDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ClientUid { get; set; }

    [Required(ErrorMessage = "Address title is required.")]
    [StringLength(100)]
    public string AddressTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full address is required.")]
    [StringLength(500)]
    public string FullAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Area is required.")]
    [StringLength(150)]
    public string Area { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }
}

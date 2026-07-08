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

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }
}

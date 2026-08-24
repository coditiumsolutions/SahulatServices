using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class ReverseGeocodeRequestDto
{
    [Range(-90, 90, ErrorMessage = "Lat must be between -90 and 90.")]
    public decimal Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Lng must be between -180 and 180.")]
    public decimal Lng { get; set; }
}

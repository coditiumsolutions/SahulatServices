using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class NearbyProvidersRequestDto
{
    [Range(-90, 90, ErrorMessage = "Lat must be between -90 and 90.")]
    public decimal Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Lng must be between -180 and 180.")]
    public decimal Lng { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryUid { get; set; }

    [Range(0.1, 500)]
    public double RadiusKm { get; set; } = 25;

    public bool OnlyAvailable { get; set; } = true;

    public bool OnlyVerified { get; set; } = true;

    [Range(1, 100)]
    public int MaxResults { get; set; } = 20;
}

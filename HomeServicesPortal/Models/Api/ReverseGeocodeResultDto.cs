namespace HomeServicesPortal.Models.Api;

public class ReverseGeocodeResultDto
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Road { get; set; }

    public string? Area { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }
}

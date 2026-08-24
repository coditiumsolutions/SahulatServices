namespace HomeServicesPortal.Models.Api;

public class NearbyProviderApiDto
{
    public int Uid { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int CategoryUid { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal AverageRating { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsVerified { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public double DistanceKm { get; set; }
}

namespace HomeServicesPortal.Models.Api;

public class ClientAddressApiDto
{
    public int Uid { get; set; }

    public int ClientUid { get; set; }

    public string AddressTitle { get; set; } = string.Empty;

    public string FullAddress { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    /// <summary>True when a real GPS pin is set. False for null coordinates and for (0,0), which every
    /// currently-published app build sends as its default and is not a valid real-world address location.</summary>
    public bool HasLocation { get; set; }
}

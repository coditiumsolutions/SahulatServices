namespace HomeServicesPortal.Models.Api;

public class ProviderAvailableStatusApiDto
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public bool IsOnline { get; set; }

    public string? AvailableFrom { get; set; }

    public string? AvailableTo { get; set; }

    public string? AvailableTiming { get; set; }
}

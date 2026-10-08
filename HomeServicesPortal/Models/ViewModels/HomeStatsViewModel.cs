namespace HomeServicesPortal.Models.ViewModels;

/// <summary>Live counts shown on the public home page (replaces hard-coded marketing numbers).</summary>
public class HomeStatsViewModel
{
    public int VerifiedProviders { get; set; }

    public int ActiveCategories { get; set; }

    public int ServiceZones { get; set; }
}

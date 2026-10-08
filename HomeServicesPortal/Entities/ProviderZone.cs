namespace HomeServicesPortal.Entities;

/// <summary>
/// One service zone a provider works in (zone names come from Configurations key=Zone).
/// Source of truth for a provider's zones; Providers.Zone mirrors them as a comma-separated
/// display string (used by the admin Assign page).
/// </summary>
public class ProviderZone
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public Provider Provider { get; set; } = null!;
}

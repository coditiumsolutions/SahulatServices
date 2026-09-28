namespace HomeServicesPortal.Entities;

/// <summary>
/// Junction row: a provider optionally offers a specific predefined ServiceTitle, within a
/// category they're already assigned (via ProviderCategories). Fully optional — unlike
/// ProviderCategories, a provider with zero rows here is a normal, unrestricted provider, not an
/// incomplete profile.
/// </summary>
public class ProviderServiceTitle
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public int ServiceTitleUid { get; set; }

    public DateTime CreatedOn { get; set; }

    public Provider Provider { get; set; } = null!;

    public ServiceTitle ServiceTitle { get; set; } = null!;
}

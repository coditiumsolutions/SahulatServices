namespace HomeServicesPortal.Entities;

/// <summary>
/// Junction row: one provider can offer service in multiple categories. Source of truth for
/// multi-category matching; Providers.CategoryUid mirrors the PrimaryCategory=1 row here and is
/// considered deprecated (kept only for backward-compat reads until callers migrate).
/// </summary>
public class ProviderCategory
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public int CategoryUid { get; set; }

    /// <summary>1 = this row is the provider's primary category; 0 otherwise.</summary>
    public int PrimaryCategory { get; set; }

    public DateTime CreatedOn { get; set; }

    public Provider Provider { get; set; } = null!;

    public ServiceCategory Category { get; set; } = null!;
}

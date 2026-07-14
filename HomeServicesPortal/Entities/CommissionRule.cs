namespace HomeServicesPortal.Entities;

public class CommissionRule
{
    public int Uid { get; set; }

    public string Scope { get; set; } = string.Empty;

    public int? CategoryUid { get; set; }

    public int? ProviderUid { get; set; }

    public string RuleType { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; }

    public ServiceCategory? Category { get; set; }

    public Provider? Provider { get; set; }
}

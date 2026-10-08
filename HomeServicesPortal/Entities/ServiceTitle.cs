namespace HomeServicesPortal.Entities;

public class ServiceTitle
{
    public int Uid { get; set; }

    public int CategoryUid { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Admin-set price estimate for this specific title, shown read-only to customers on the request form and used to populate CustomerServiceRequests.EstimatedBudget server-side.</summary>
    public decimal? BasePrice { get; set; }

    /// <summary>Free-text estimate shown to customers (e.g. "2000-3000" or "From 1500"). Display only; BasePrice stays the numeric value used for billing logic.</summary>
    public string? EstimateText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; }

    public ServiceCategory Category { get; set; } = null!;
}

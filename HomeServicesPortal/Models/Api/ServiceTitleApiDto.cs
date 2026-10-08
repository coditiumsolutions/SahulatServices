namespace HomeServicesPortal.Models.Api;

public class ServiceTitleApiDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Admin-set price estimate for this title (ServiceTitles.BasePrice). Null if not set. Show as the read-only estimated budget on the service-request form.</summary>
    public decimal? BasePrice { get; set; }

    /// <summary>Free-text estimate (ServiceTitles.EstimateText), e.g. "2000-3000". Null if not set. Prefer showing this over BasePrice when present.</summary>
    public string? EstimateText { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime? CreatedOn { get; set; }
}

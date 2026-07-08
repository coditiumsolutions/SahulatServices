namespace HomeServicesPortal.Models.Api;

/// <summary>Service request returned to providers filtered by matching category.</summary>
public class ProviderServiceRequestApiDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerMobile { get; set; }

    /// <summary>Short title derived from category and request id (maps to user-facing request title).</summary>
    public string RequestTitle { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ServiceAddress { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? RequestDate { get; set; }

    public string? Status { get; set; }
}

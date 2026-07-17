namespace HomeServicesPortal.Models.Api;

public class CustomerServiceRequestApiDto
{
    public int Uid { get; set; }

    public int ClientUid { get; set; }

    public string? ClientName { get; set; }

    public int CategoryUid { get; set; }

    public string? CategoryName { get; set; }

    public int ClientAddressUid { get; set; }

    public string? AddressTitle { get; set; }

    public string ServiceTitle { get; set; } = string.Empty;

    public string? ServiceDescription { get; set; }

    public DateOnly? PreferredServiceDate { get; set; }

    public string? PreferredServiceTime { get; set; }

    public bool IsUrgent { get; set; }

    public string? ContactPerson { get; set; }

    public string ContactNo { get; set; } = string.Empty;

    public decimal? EstimatedBudget { get; set; }

    public string Status { get; set; } = "Pending";

    public string? Remarks { get; set; }

    public DateTime CreatedOn { get; set; }
}

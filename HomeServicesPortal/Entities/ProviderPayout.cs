namespace HomeServicesPortal.Entities;

public class ProviderPayout
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "Pending";

    public string? Method { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? PaidOn { get; set; }

    public Provider Provider { get; set; } = null!;
}

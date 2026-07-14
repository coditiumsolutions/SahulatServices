namespace HomeServicesPortal.Entities;

public class ServiceBooking
{
    public int Uid { get; set; }

    public int RequestUid { get; set; }

    public int ClientUid { get; set; }

    public int ProviderUid { get; set; }

    public decimal FinalAmount { get; set; }

    public string PaymentMode { get; set; } = string.Empty;

    public string CommissionType { get; set; } = string.Empty;

    public decimal CommissionValue { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal ProviderEarning { get; set; }

    public string Status { get; set; } = "Completed";

    public DateTime CreatedOn { get; set; }

    public CustomerServiceRequest Request { get; set; } = null!;

    public Client Client { get; set; } = null!;

    public Provider Provider { get; set; } = null!;
}

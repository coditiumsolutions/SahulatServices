namespace HomeServicesPortal.Entities;

public class ServiceBooking
{
    public int Uid { get; set; }

    public int RequestUid { get; set; }

    public int ClientUid { get; set; }

    public int ProviderUid { get; set; }

    public string? ServiceDetail { get; set; }

    public decimal EstimatedAmount { get; set; }

    public decimal VisitCharges { get; set; }

    public decimal AdditionalCharges { get; set; }

    public decimal Deductions { get; set; }

    public decimal FinalAmount { get; set; }

    public decimal CustomerPaid { get; set; }

    public string PaymentMode { get; set; } = string.Empty;

    public decimal CustomerRemaining { get; set; }

    public string CommissionType { get; set; } = string.Empty;

    public decimal CommissionValue { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal ProviderEarning { get; set; }

    public string Status { get; set; } = "Completed";

    public string? Passcode { get; set; }

    public DateTime? AcceptedOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public DateTime CreatedOn { get; set; }

    public CustomerServiceRequest Request { get; set; } = null!;

    public Client Client { get; set; } = null!;

    public Provider Provider { get; set; } = null!;
}

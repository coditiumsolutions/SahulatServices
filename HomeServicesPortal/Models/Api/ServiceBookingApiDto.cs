namespace HomeServicesPortal.Models.Api;

public class ServiceBookingApiDto
{
    public int Uid { get; set; }

    public int RequestUid { get; set; }

    public string? RequestTitle { get; set; }

    public int ClientUid { get; set; }

    public string? ClientName { get; set; }

    public int ProviderUid { get; set; }

    public string? ProviderName { get; set; }

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

    public string Status { get; set; } = string.Empty;

    public string? Passcode { get; set; }

    public DateTime? AcceptedOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? ProviderMobileNo { get; set; }

    public string? ProviderProfilePhotoPath { get; set; }

    public string? ProviderCnic { get; set; }

    public string? ClientMobileNo { get; set; }

    public string? ClientAddressTitle { get; set; }

    public string? ClientFullAddress { get; set; }

    public string? ClientArea { get; set; }

    public string? ClientCity { get; set; }
}

namespace HomeServicesPortal.Entities;

public class PaymentLedger
{
    public int Uid { get; set; }

    public int? BookingUid { get; set; }

    public string AccountType { get; set; } = string.Empty;

    public int? ProviderUid { get; set; }

    public string EntryType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public ServiceBooking? Booking { get; set; }

    public Provider? Provider { get; set; }
}

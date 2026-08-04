namespace HomeServicesPortal.Models.Api;

public class VerifyCompletionPasscodeDto
{
    public int ProviderUid { get; set; }

    public string Passcode { get; set; } = string.Empty;

    public decimal ActualAmountPaid { get; set; }

    public string? PaymentMode { get; set; }
}

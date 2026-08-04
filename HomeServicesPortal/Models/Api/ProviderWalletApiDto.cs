namespace HomeServicesPortal.Models.Api;

public class ProviderWalletApiDto
{
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Current net balance: positive = company owes provider, negative = provider owes company (unremitted cash commission).</summary>
    public decimal Balance { get; set; }

    /// <summary>Sum of Pending ProviderPayout rows awaiting admin disbursement.</summary>
    public decimal PendingPayoutTotal { get; set; }

    public List<ProviderWalletTxnApiDto> Transactions { get; set; } = new();
}

public class ProviderWalletTxnApiDto
{
    public int Uid { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? BookingUid { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Signed amount: Credit = +, Debit = −</summary>
    public decimal SignedAmount { get; set; }
    public decimal RunningBalance { get; set; }
}

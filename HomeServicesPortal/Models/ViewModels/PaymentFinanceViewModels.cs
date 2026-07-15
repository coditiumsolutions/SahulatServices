namespace HomeServicesPortal.Models.ViewModels;

public class PaymentLedgerListVm
{
    public List<PaymentLedgerItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class PaymentLedgerItemVm
{
    public int Uid { get; set; }
    public int? BookingUid { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class PaymentLedgerDetailsVm
{
    public int Uid { get; set; }
    public int? BookingUid { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public int? ProviderUid { get; set; }
    public string? ProviderName { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class ProviderPayoutListVm
{
    public List<ProviderPayoutItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ProviderPayoutItemVm
{
    public int Uid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Method { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? PaidOn { get; set; }
}

public class ProviderPayoutDetailsVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Method { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? PaidOn { get; set; }
}

public class PersonLedgerIndexVm
{
    public string? Search { get; set; }
    public List<PersonLedgerPersonVm> People { get; set; } = new();
}

public class PersonLedgerPersonVm
{
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalPlus { get; set; }
    public decimal TotalMinus { get; set; }
    public decimal Balance { get; set; }
}

public class PersonLedgerStatementVm
{
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalPlus { get; set; }
    public decimal TotalMinus { get; set; }
    public decimal Balance { get; set; }
    public List<PersonLedgerTxnVm> Transactions { get; set; } = new();
    public PersonLedgerAddEntryVm AddEntry { get; set; } = new();
}

public class PersonLedgerTxnVm
{
    public int Uid { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? BookingUid { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Signed amount: Credit = +, Debit = −</summary>
    public decimal SignedAmount { get; set; }
    public decimal RunningBalance { get; set; }
}

public class PersonLedgerAddEntryVm
{
    public int ProviderUid { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Display(Name = "Entry Type")]
    public string EntryType { get; set; } = "Credit";

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(30)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    public int? BookingUid { get; set; }
}

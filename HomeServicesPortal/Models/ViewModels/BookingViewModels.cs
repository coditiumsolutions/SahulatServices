using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class BookingListVm
{
    public List<BookingItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class BookingItemVm
{
    public int Uid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? Status { get; set; }
}

public class BookingFormVm
{
    public int Uid { get; set; }

    /// <summary>When true, Service Request / Client / Service Type are display-only.</summary>
    public bool LockRequestFields { get; set; }

    [Required(ErrorMessage = "Service request is required.")]
    [Display(Name = "Service Request")]
    public int RequestUid { get; set; }

    [Display(Name = "Service Request")]
    public string ServiceTitle { get; set; } = string.Empty;

    [Display(Name = "Client Name")]
    public string ClientName { get; set; } = string.Empty;

    [Display(Name = "Service Type")]
    public string ServiceType { get; set; } = string.Empty;

    [Display(Name = "Service Detail")]
    [StringLength(1000)]
    public string? ServiceDetail { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Required]
    [Display(Name = "Estimated Amount")]
    [Range(0, double.MaxValue)]
    public decimal EstimatedAmount { get; set; }

    [Required]
    [Display(Name = "Visit Charges")]
    [Range(0, double.MaxValue)]
    public decimal VisitCharges { get; set; }

    [Required]
    [Display(Name = "Additional Charges")]
    [Range(0, double.MaxValue)]
    public decimal AdditionalCharges { get; set; }

    [Required]
    [Display(Name = "Deductions")]
    [Range(0, double.MaxValue)]
    public decimal Deductions { get; set; }

    [Required(ErrorMessage = "Final bill is required.")]
    [Display(Name = "Final Bill")]
    [Range(0, double.MaxValue)]
    public decimal FinalAmount { get; set; }

    [Required]
    [Display(Name = "Customer Paid")]
    [Range(0, double.MaxValue)]
    public decimal CustomerPaid { get; set; }

    [Required(ErrorMessage = "Payment method is required.")]
    [StringLength(30)]
    [Display(Name = "Payment Method")]
    public string PaymentMode { get; set; } = "CashToProvider";

    [Display(Name = "Customer Remaining")]
    public decimal CustomerRemaining { get; set; }

    [Required(ErrorMessage = "Commission type is required.")]
    [StringLength(10)]
    [Display(Name = "Commission Type")]
    public string CommissionType { get; set; } = "Percent";

    [Required(ErrorMessage = "Commission rate / value is required.")]
    [Display(Name = "Commission Rate / Value")]
    [Range(0, double.MaxValue)]
    public decimal CommissionValue { get; set; }

    [Display(Name = "Company Commission")]
    [Range(0, double.MaxValue)]
    public decimal? CommissionAmount { get; set; }

    [Display(Name = "Provider Earning")]
    [Range(0, double.MaxValue)]
    public decimal? ProviderEarning { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Completed";

    public List<SelectListItem> Requests { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
    public List<SelectListItem> PaymentModeOptions { get; set; } = new();
    public List<SelectListItem> CommissionTypeOptions { get; set; } = new();
}

public class BookingDetailsVm
{
    public int Uid { get; set; }
    public int RequestUid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public string ServiceTitle { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string? ServiceDetail { get; set; }
    public int ClientUid { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
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
    public string? Status { get; set; }
    public int LedgerCount { get; set; }
}

public class BookingDeleteVm
{
    public int Uid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public string? Status { get; set; }
}

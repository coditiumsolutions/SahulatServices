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

    [Required(ErrorMessage = "Service request is required.")]
    [Display(Name = "Service Request")]
    public int RequestUid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Required(ErrorMessage = "Final amount is required.")]
    [Display(Name = "Final Amount")]
    [Range(0.01, double.MaxValue)]
    public decimal FinalAmount { get; set; }

    [Required(ErrorMessage = "Payment mode is required.")]
    [StringLength(30)]
    [Display(Name = "Payment Mode")]
    public string PaymentMode { get; set; } = "CashToProvider";

    [Required(ErrorMessage = "Commission type is required.")]
    [StringLength(10)]
    [Display(Name = "Commission Type")]
    public string CommissionType { get; set; } = "Percent";

    [Required(ErrorMessage = "Commission value is required.")]
    [Display(Name = "Commission Value")]
    [Range(0, double.MaxValue)]
    public decimal CommissionValue { get; set; }

    [Display(Name = "Commission Amount")]
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
    public int ClientUid { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
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

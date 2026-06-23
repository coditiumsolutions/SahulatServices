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
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public decimal? FinalAmount { get; set; }
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

    [Display(Name = "Final Amount")]
    [Range(0, double.MaxValue)]
    public decimal? FinalAmount { get; set; }

    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Accepted";

    public List<SelectListItem> Requests { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
}

public class BookingDetailsVm
{
    public int Uid { get; set; }
    public int RequestUid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public decimal? FinalAmount { get; set; }
    public string? Status { get; set; }
    public int TrackingCount { get; set; }
    public int PaymentCount { get; set; }
    public int ReviewCount { get; set; }
}

public class BookingDeleteVm
{
    public int Uid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public string? Status { get; set; }
}

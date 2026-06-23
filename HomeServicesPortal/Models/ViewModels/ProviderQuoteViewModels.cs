using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ProviderQuoteListVm
{
    public List<ProviderQuoteItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ProviderQuoteItemVm
{
    public int Uid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public decimal? QuoteAmount { get; set; }
    public int? EstimatedArrivalMinutes { get; set; }
    public decimal? DistanceKm { get; set; }
    public DateTime? QuoteDate { get; set; }
}

public class ProviderQuoteFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Service request is required.")]
    [Display(Name = "Service Request")]
    public int RequestUid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Display(Name = "Quote Amount")]
    [Range(0, double.MaxValue)]
    public decimal? QuoteAmount { get; set; }

    [Display(Name = "Estimated Arrival (Minutes)")]
    [Range(0, 1440)]
    public int? EstimatedArrivalMinutes { get; set; }

    [Display(Name = "Distance (KM)")]
    [Range(0, double.MaxValue)]
    public decimal? DistanceKm { get; set; }

    [StringLength(500)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    public List<SelectListItem> Requests { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
}

public class ProviderQuoteDetailsVm
{
    public int Uid { get; set; }
    public int RequestUid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal? QuoteAmount { get; set; }
    public int? EstimatedArrivalMinutes { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? Remarks { get; set; }
    public DateTime? QuoteDate { get; set; }
}

public class ProviderQuoteDeleteVm
{
    public int Uid { get; set; }
    public string RequestLabel { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public decimal? QuoteAmount { get; set; }
    public DateTime? QuoteDate { get; set; }
}

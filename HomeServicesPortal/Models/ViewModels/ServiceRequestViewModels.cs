using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ServiceRequestListVm
{
    public List<ServiceRequestItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ServiceRequestItemVm
{
    public int Uid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public string? Status { get; set; }
    public DateTime? RequestDate { get; set; }
}

public class ServiceRequestFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerUid { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public int CategoryUid { get; set; }

    [StringLength(500)]
    [Display(Name = "Service Address")]
    public string? ServiceAddress { get; set; }

    [Display(Name = "Latitude")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public decimal? Longitude { get; set; }

    [Display(Name = "Problem Description")]
    public string? ProblemDescription { get; set; }

    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Pending";

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
}

public class ServiceRequestDetailsVm
{
    public int Uid { get; set; }
    public int CustomerUid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CategoryUid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ProblemDescription { get; set; }
    public DateTime? RequestDate { get; set; }
    public string? Status { get; set; }
    public int QuoteCount { get; set; }
    public int BookingCount { get; set; }
}

public class ServiceRequestDeleteVm
{
    public int Uid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public string? Status { get; set; }
    public DateTime? RequestDate { get; set; }
}

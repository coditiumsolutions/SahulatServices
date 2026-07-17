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
    public string? ServiceTitle { get; set; }
    public string? Status { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime? RequestDate { get; set; }
}

public class ServiceRequestFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Client is required.")]
    [Display(Name = "Client")]
    public int CustomerUid { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public int CategoryUid { get; set; }

    [Required(ErrorMessage = "Client address is required.")]
    [Display(Name = "Client Address")]
    public int ClientAddressUid { get; set; }

    [Required(ErrorMessage = "Service title is required.")]
    [StringLength(150)]
    [Display(Name = "Service Title")]
    public string ServiceTitle { get; set; } = string.Empty;

    [Display(Name = "Service Description")]
    public string? ServiceDescription { get; set; }

    [Display(Name = "Preferred Date")]
    [DataType(DataType.Date)]
    public DateOnly? PreferredServiceDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Preferred Time")]
    public string? PreferredServiceTime { get; set; }

    [Display(Name = "Urgent")]
    public bool IsUrgent { get; set; }

    [StringLength(150)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(20)]
    [Display(Name = "Contact No")]
    public string ContactNo { get; set; } = string.Empty;

    [Display(Name = "Estimated Budget")]
    public decimal? EstimatedBudget { get; set; }

    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Addresses { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
}

public class ServiceRequestDetailsVm
{
    public int Uid { get; set; }
    public int CustomerUid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CategoryUid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ClientAddressUid { get; set; }
    public string? ServiceAddress { get; set; }
    public string ServiceTitle { get; set; } = string.Empty;
    public string? ServiceDescription { get; set; }
    public DateOnly? PreferredServiceDate { get; set; }
    public string? PreferredServiceTime { get; set; }
    public bool IsUrgent { get; set; }
    public string? ContactPerson { get; set; }
    public string ContactNo { get; set; } = string.Empty;
    public decimal? EstimatedBudget { get; set; }
    public DateTime? RequestDate { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
    public int? BookingUid { get; set; }
}

public class ServiceRequestDeleteVm
{
    public int Uid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ServiceTitle { get; set; }
    public string? ServiceAddress { get; set; }
    public string? Status { get; set; }
    public DateTime? RequestDate { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ReviewListVm
{
    public List<ReviewItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ReviewItemVm
{
    public int Uid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateTime? ReviewDate { get; set; }
}

public class ReviewFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Booking is required.")]
    [Display(Name = "Booking")]
    public int BookingUid { get; set; }

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerUid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Display(Name = "Rating")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int? Rating { get; set; }

    [StringLength(1000)]
    [Display(Name = "Review Text")]
    public string? ReviewText { get; set; }

    public List<SelectListItem> Bookings { get; set; } = new();
    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
}

public class ReviewDetailsVm
{
    public int Uid { get; set; }
    public int BookingUid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public int CustomerUid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime? ReviewDate { get; set; }
}

public class ReviewDeleteVm
{
    public int Uid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateTime? ReviewDate { get; set; }
}

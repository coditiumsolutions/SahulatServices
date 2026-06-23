using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class BookingTrackingListVm
{
    public List<BookingTrackingItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class BookingTrackingItemVm
{
    public int Uid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime? StatusDate { get; set; }
}

public class BookingTrackingFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Booking is required.")]
    [Display(Name = "Booking")]
    public int BookingUid { get; set; }

    [StringLength(50)]
    [Display(Name = "Status")]
    public string? Status { get; set; }

    [StringLength(500)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    public List<SelectListItem> Bookings { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();
}

public class BookingTrackingDetailsVm
{
    public int Uid { get; set; }
    public int BookingUid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime? StatusDate { get; set; }
}

public class BookingTrackingDeleteVm
{
    public int Uid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTime? StatusDate { get; set; }
}

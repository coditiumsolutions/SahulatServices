using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class PaymentListVm
{
    public List<PaymentItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class PaymentItemVm
{
    public int Uid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionNo { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class PaymentFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Booking is required.")]
    [Display(Name = "Booking")]
    public int BookingUid { get; set; }

    [Display(Name = "Amount")]
    [Range(0, double.MaxValue)]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    [Display(Name = "Payment Method")]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    [Display(Name = "Transaction No")]
    public string? TransactionNo { get; set; }

    public List<SelectListItem> Bookings { get; set; } = new();
    public List<SelectListItem> PaymentMethods { get; set; } = new();
}

public class PaymentDetailsVm
{
    public int Uid { get; set; }
    public int BookingUid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionNo { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class PaymentDeleteVm
{
    public int Uid { get; set; }
    public string BookingLabel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
}

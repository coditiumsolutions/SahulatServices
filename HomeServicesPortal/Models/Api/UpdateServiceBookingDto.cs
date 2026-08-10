using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class UpdateServiceBookingDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int BookingUid { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ProviderUid { get; set; }

    [StringLength(1000)]
    public string? ServiceDetail { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EstimatedAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal VisitCharges { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AdditionalCharges { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Deductions { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CustomerPaid { get; set; }

    [Required(ErrorMessage = "Payment mode is required.")]
    [StringLength(30)]
    public string PaymentMode { get; set; } = "CashToProvider";

    [Required(ErrorMessage = "Commission type is required.")]
    [StringLength(10)]
    public string CommissionType { get; set; } = "Percent";

    [Range(0, double.MaxValue)]
    public decimal CommissionValue { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(20)]
    public string Status { get; set; } = "Accepted";

    /// <summary>Required when transitioning an Accepted/In Progress booking to Cancelled.</summary>
    [StringLength(500)]
    public string? CancelReason { get; set; }
}

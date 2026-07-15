using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class AssignProviderVm
{
    public int RequestUid { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string ServiceTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public string? Status { get; set; }
    public decimal? EstimatedBudget { get; set; }

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
    public decimal CommissionAmount { get; set; }

    [Display(Name = "Provider Earning")]
    [Range(0, double.MaxValue)]
    public decimal ProviderEarning { get; set; }

    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> PaymentModeOptions { get; set; } = new();
    public List<SelectListItem> CommissionTypeOptions { get; set; } = new();
}

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

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Required(ErrorMessage = "Total amount is required.")]
    [Display(Name = "Total Amount")]
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

    [Required(ErrorMessage = "Commission rate / value is required.")]
    [Display(Name = "Commission Rate / Value")]
    [Range(0, double.MaxValue)]
    public decimal CommissionValue { get; set; }

    [Display(Name = "Commission Amount")]
    [Range(0, double.MaxValue)]
    public decimal? CommissionAmount { get; set; }

    [Display(Name = "Provider Earning")]
    [Range(0, double.MaxValue)]
    public decimal? ProviderEarning { get; set; }

    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> PaymentModeOptions { get; set; } = new();
    public List<SelectListItem> CommissionTypeOptions { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class AssignProviderVm
{
    public int RequestUid { get; set; }

    public int CategoryUid { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string? ClientCity { get; set; }
    public string ServiceTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public string? Status { get; set; }
    public decimal? EstimatedBudget { get; set; }

    /// <summary>ProviderUid → Zone shown next to each Assign provider checkbox.</summary>
    public Dictionary<int, string> ProviderZones { get; set; } = new();

    [Display(Name = "Service Detail")]
    [StringLength(1000)]
    public string? ServiceDetail { get; set; }

    [Display(Name = "Providers")]
    [MinLength(1, ErrorMessage = "Select at least one provider.")]
    public List<int> ProviderUids { get; set; } = new();

    [Display(Name = "Show city providers (override category match)")]
    public bool ShowAllProviders { get; set; }

    public bool HasCategoryMatch { get; set; }

    /// <summary>
    /// True when ServiceTitle text matched a predefined ServiceTitles row for this request's
    /// category AND at least one eligible provider had that title, so Providers below was
    /// narrowed by title (not just category). False means category-only filtering applied —
    /// either because ServiceTitle is free text with no predefined match, or because no
    /// provider had the matched title (falls back to the category list rather than showing empty).
    /// </summary>
    public bool TitleFiltered { get; set; }

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

    /// <summary>Where the prefilled commission came from (Commission Rules scope or default).</summary>
    public string CommissionSourceLabel { get; set; } = string.Empty;

    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> AllProviders { get; set; } = new();
    public List<SelectListItem> PaymentModeOptions { get; set; } = new();
    public List<SelectListItem> CommissionTypeOptions { get; set; } = new();
}

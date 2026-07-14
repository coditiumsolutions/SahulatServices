using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class CommissionRuleListVm
{
    public List<CommissionRuleItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class CommissionRuleItemVm
{
    public int Uid { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? ProviderName { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public class CommissionRuleFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Scope is required.")]
    [Display(Name = "Scope")]
    public string Scope { get; set; } = "Global";

    [Display(Name = "Category")]
    public int? CategoryUid { get; set; }

    [Display(Name = "Provider")]
    public int? ProviderUid { get; set; }

    [Required(ErrorMessage = "Rule type is required.")]
    [Display(Name = "Rule Type")]
    public string RuleType { get; set; } = "Percentage";

    [Required(ErrorMessage = "Value is required.")]
    [Display(Name = "Value")]
    [Range(0.01, double.MaxValue)]
    public decimal Value { get; set; }

    [Required(ErrorMessage = "Effective from is required.")]
    [Display(Name = "Effective From")]
    [DataType(DataType.Date)]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    [Display(Name = "Effective To")]
    [DataType(DataType.Date)]
    public DateTime? EffectiveTo { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> ScopeOptions { get; set; } = new();
    public List<SelectListItem> RuleTypeOptions { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Providers { get; set; } = new();
}

public class CommissionRuleDetailsVm
{
    public int Uid { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? ProviderName { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CommissionRuleDeleteVm
{
    public int Uid { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ClientServiceRequestFormVm
{
    public int ServiceUid { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public int CategoryUid { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Estimated Budget")]
    public string EstimatedBudgetDisplay { get; set; } = "Select a service title";

    [Required(ErrorMessage = "Please select a service address.")]
    [Display(Name = "Service Address")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a service address.")]
    public int? ClientAddressUid { get; set; }

    /// <summary>Selected ServiceTitles.UID, or <see cref="OtherTitleValue"/> for "Other (not listed)". 0 = nothing chosen.</summary>
    [Display(Name = "Service Title")]
    public int? ServiceTitleUid { get; set; }

    public const int OtherTitleValue = -1;

    public bool IsOtherTitle => ServiceTitleUid == OtherTitleValue;

    [Display(Name = "Specify Service")]
    [StringLength(150)]
    public string? OtherServiceTitle { get; set; }

    [Display(Name = "Service Description")]
    [StringLength(4000)]
    public string? ServiceDescription { get; set; }

    [Display(Name = "Same as my account")]
    public bool SameAsAccount { get; set; } = true;

    [Display(Name = "On-site Contact Person")]
    [StringLength(150)]
    public string? ContactPerson { get; set; }

    [Display(Name = "On-site Contact Number")]
    [StringLength(20)]
    public string? ContactNo { get; set; }

    [Display(Name = "Remarks (optional)")]
    [StringLength(500)]
    public string? Remarks { get; set; }

    [Display(Name = "Preferred Date")]
    public DateOnly? PreferredServiceDate { get; set; }

    /// <summary>24-hour HH:mm from the time input.</summary>
    [Display(Name = "Preferred Time")]
    [StringLength(5)]
    public string? PreferredServiceTime { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string AccountMobile { get; set; } = string.Empty;

    public string MinDate { get; set; } = string.Empty;

    public string MaxDate { get; set; } = string.Empty;

    [Display(Name = "Mark as Urgent")]
    public bool IsUrgent { get; set; }

    public IEnumerable<SelectListItem> Addresses { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Titles { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>JSON map of titleUid → { basePrice, estimateText, title } for the read-only budget field.</summary>
    public string TitlesBudgetJson { get; set; } = "{}";

    public bool HasAddresses => Addresses.Any(a => !string.IsNullOrEmpty(a.Value));

    public bool HasTitles => Titles.Any(a => !string.IsNullOrEmpty(a.Value));
}

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
    public int ClientAddressUid { get; set; }

    [Required(ErrorMessage = "Please select a service title.")]
    [Display(Name = "Service Title")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a service title.")]
    public int ServiceTitleUid { get; set; }

    [Display(Name = "Service Description")]
    [StringLength(4000)]
    public string? ServiceDescription { get; set; }

    [Display(Name = "Mark as Urgent")]
    public bool IsUrgent { get; set; }

    public IEnumerable<SelectListItem> Addresses { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Titles { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>JSON map of titleUid → { basePrice, estimateText, title } for the read-only budget field.</summary>
    public string TitlesBudgetJson { get; set; } = "{}";

    public bool HasAddresses => Addresses.Any(a => !string.IsNullOrEmpty(a.Value));

    public bool HasTitles => Titles.Any(a => !string.IsNullOrEmpty(a.Value));
}

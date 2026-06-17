using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ProviderAvailabilityListVm
{
    public List<ProviderAvailabilityItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "provider";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ProviderAvailabilityItemVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool? IsOnline { get; set; }
    public TimeOnly? AvailableFrom { get; set; }
    public TimeOnly? AvailableTo { get; set; }
}

public class ProviderAvailabilityFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Display(Name = "Online")]
    public bool IsOnline { get; set; }

    [Display(Name = "Available From")]
    public TimeOnly? AvailableFrom { get; set; }

    [Display(Name = "Available To")]
    public TimeOnly? AvailableTo { get; set; }

    public List<SelectListItem> Providers { get; set; } = new();
}

public class ProviderAvailabilityDetailsVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool? IsOnline { get; set; }
    public TimeOnly? AvailableFrom { get; set; }
    public TimeOnly? AvailableTo { get; set; }
}

public class ProviderAvailabilityDeleteVm
{
    public int Uid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool? IsOnline { get; set; }
    public TimeOnly? AvailableFrom { get; set; }
    public TimeOnly? AvailableTo { get; set; }
}

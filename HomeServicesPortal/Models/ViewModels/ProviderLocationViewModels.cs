using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ProviderLocationListVm
{
    public List<ProviderLocationItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "provider";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ProviderLocationItemVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class ProviderLocationFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Required(ErrorMessage = "Latitude is required.")]
    [Display(Name = "Latitude")]
    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required.")]
    [Display(Name = "Longitude")]
    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public List<SelectListItem> Providers { get; set; } = new();
}

public class ProviderLocationDetailsVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class ProviderLocationDeleteVm
{
    public int Uid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

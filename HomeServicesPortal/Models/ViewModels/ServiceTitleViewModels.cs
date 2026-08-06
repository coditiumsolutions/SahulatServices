using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ServiceTitleListVm
{
    public List<ServiceTitleItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ServiceTitleItemVm
{
    public int Uid { get; set; }
    public int CategoryUid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceTitleFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Parent category is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Parent category is required.")]
    [Display(Name = "Category")]
    public int CategoryUid { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Categories { get; set; } = new();
}

public class ServiceTitleDetailsVm
{
    public int Uid { get; set; }
    public int CategoryUid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceTitleDeleteVm
{
    public int Uid { get; set; }
    public int CategoryUid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

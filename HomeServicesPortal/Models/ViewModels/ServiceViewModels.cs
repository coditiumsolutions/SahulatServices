using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

public class ServiceListVm
{
    public List<ServiceItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ServiceItemVm
{
    public int Uid { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Service name is required.")]
    [StringLength(150)]
    [Display(Name = "Service Name")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
}

public class ServiceDetailsVm
{
    public int Uid { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceDeleteVm
{
    public int Uid { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

public class ConfigurationListVm
{
    public List<ConfigurationItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ConfigurationItemVm
{
    public int Uid { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public int ValueCount { get; set; }
}

public class ConfigurationFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Config key is required.")]
    [StringLength(100)]
    [Display(Name = "Config Key")]
    public string ConfigKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Config value is required.")]
    [Display(Name = "Config Value")]
    public string ConfigValue { get; set; } = string.Empty;
}

public class ConfigurationDetailsVm
{
    public int Uid { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public List<string> Values { get; set; } = new();
}

public class ConfigurationDeleteVm
{
    public int Uid { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
}

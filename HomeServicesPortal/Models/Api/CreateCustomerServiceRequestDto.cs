using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class CreateCustomerServiceRequestDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ClientUid { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int CategoryUid { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ClientAddressUid { get; set; }

    [Required(ErrorMessage = "Service title is required.")]
    [StringLength(150)]
    public string ServiceTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Service description is required.")]
    [StringLength(4000)]
    public string ServiceDescription { get; set; } = string.Empty;

    public DateOnly? PreferredServiceDate { get; set; }

    [StringLength(50)]
    public string? PreferredServiceTime { get; set; }

    public bool IsUrgent { get; set; }

    [StringLength(150)]
    public string? ContactPerson { get; set; }

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(20)]
    public string ContactNo { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal? EstimatedBudget { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class UpdateClientDetailRequestDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ClientUid { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Cnic { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class UpdateProviderZonesRequestDto
{
    /// <summary>Full replacement set of zone names (each must be one of GET /api/zones). Send an empty list to clear.</summary>
    [Required]
    public List<string> Zones { get; set; } = new();
}

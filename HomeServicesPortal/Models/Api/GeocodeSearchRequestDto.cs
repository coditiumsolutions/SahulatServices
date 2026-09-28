using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class GeocodeSearchRequestDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Search query is required.")]
    public string? Q { get; set; }

    [Range(1, 10, ErrorMessage = "Limit must be between 1 and 10.")]
    public int Limit { get; set; } = 5;
}

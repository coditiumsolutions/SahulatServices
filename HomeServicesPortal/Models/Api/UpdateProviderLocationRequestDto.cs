using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class UpdateProviderLocationRequestDto
{
    /// <summary>Device GPS-fix time (UTC) — drives the idempotent/ordering write guard.</summary>
    [Required]
    public DateTime ClientTimestampUtc { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal Longitude { get; set; }

    /// <summary>Optional — if set, also broadcasts the update to that booking's SignalR group.</summary>
    public int? BookingUid { get; set; }
}

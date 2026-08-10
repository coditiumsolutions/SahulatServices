using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

public class RespondToBookingDto
{
    public int ProviderUid { get; set; }

    public bool Accept { get; set; }

    /// <summary>Required when Accept is false. Reason the provider is rejecting this booking.</summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

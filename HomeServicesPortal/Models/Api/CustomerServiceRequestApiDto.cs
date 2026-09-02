namespace HomeServicesPortal.Models.Api;

public class CustomerServiceRequestApiDto
{
    public int Uid { get; set; }

    public int ClientUid { get; set; }

    public string? ClientName { get; set; }

    public int CategoryUid { get; set; }

    public string? CategoryName { get; set; }

    public int ClientAddressUid { get; set; }

    public string? AddressTitle { get; set; }

    public string ServiceTitle { get; set; } = string.Empty;

    public string? ServiceDescription { get; set; }

    public DateOnly? PreferredServiceDate { get; set; }

    public string? PreferredServiceTime { get; set; }

    public bool IsUrgent { get; set; }

    public string? ContactPerson { get; set; }

    public string ContactNo { get; set; } = string.Empty;

    public decimal? EstimatedBudget { get; set; }

    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Computed, read-only client progress-bar stage: Requested / Assigned / In Progress /
    /// Completed. Never persisted — derived from the linked booking's status (and, for the
    /// Accepted->In Progress transition, whether the preferred service date/time has arrived).
    /// Null whenever the request or its booking is cancelled — the client app should hide the
    /// progress bar and show a plain "Cancelled" label instead of switching on this field.
    /// See docs/status-workflow.md.
    /// </summary>
    public string? ProgressStatus { get; set; }

    public string? Remarks { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedOn { get; set; }

    public int? ProviderUid { get; set; }

    public string? ProviderName { get; set; }

    public string? ProviderMobileNo { get; set; }

    public string? ProviderProfilePhotoPath { get; set; }

    public string? ProviderCnic { get; set; }

    public string? Passcode { get; set; }
}

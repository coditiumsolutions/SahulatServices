namespace HomeServicesPortal.Helpers;

/// <summary>
/// Status values a client is allowed to set on their own CustomerServiceRequests row via the
/// mobile PUT endpoint. Deliberately narrower than the admin portal's own whitelist
/// (Services/ServiceRequestService.ValidStatuses) — a client can edit while Pending or cancel,
/// but Assigned/In Progress/Completed are staff/provider/system-driven only.
/// See docs/status-workflow.md.
/// </summary>
public static class RequestStatusConstants
{
    public static readonly string[] ClientEditableStatuses = ["Pending", "Cancelled"];
}

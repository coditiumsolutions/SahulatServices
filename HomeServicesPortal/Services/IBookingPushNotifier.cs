namespace HomeServicesPortal.Services;

/// <summary>
/// Turns booking lifecycle events into client/provider notifications (inbox row + push). Call only AFTER the
/// state change has actually been applied, and never on an idempotent retry — see AGENTS.md "Idempotency".
/// Every method is a no-op when Notifications:BookingPushEnabled is false.
/// </summary>
public interface IBookingPushNotifier
{
    /// <summary>Staff assigned provider(s): every Pending booking of the request gets a "new job" notification.</summary>
    Task JobAssignedAsync(int requestUid, CancellationToken cancellationToken = default);

    /// <summary>A provider accepted: tell the client, and tell the siblings that lost out.</summary>
    Task BookingAcceptedAsync(int bookingUid, IReadOnlyCollection<(int BookingUid, int ProviderUid)> losingSiblings,
        CancellationToken cancellationToken = default);

    /// <summary>Request went back to Initiated (all providers rejected, or an accepted provider cancelled).</summary>
    Task ProviderReassigningAsync(int requestUid, CancellationToken cancellationToken = default);

    /// <summary>Staff cancelled an accepted / in-progress booking: tell both client and provider.</summary>
    Task BookingCancelledByStaffAsync(int bookingUid, CancellationToken cancellationToken = default);

    Task JobStartedAsync(int bookingUid, CancellationToken cancellationToken = default);

    Task JobCompletedAsync(int bookingUid, CancellationToken cancellationToken = default);
}

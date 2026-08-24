using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Hubs;

/// <summary>
/// Real-time provider-location broadcast. Clients subscribe per-booking via
/// JoinBookingGroup/LeaveBookingGroup ("booking-{BookingUid}" groups); providers push their
/// position via PushLocation, which writes through the same idempotent
/// IProviderLocationQueryService.UpdateCurrentLocationAsync used by the REST fallback endpoint
/// (see ProviderLocationsApiController), so duplicate/out-of-order pushes never regress the
/// stored position and never re-broadcast a stale update.
/// </summary>
[Authorize]
public class LocationTrackingHub : Hub
{
    private readonly IProviderLocationQueryService _locationService;
    private readonly AppDbContext _db;

    public LocationTrackingHub(IProviderLocationQueryService locationService, AppDbContext db)
    {
        _locationService = locationService;
        _db = db;
    }

    /// <summary>Provider pushes its current position while actively working a booking.</summary>
    /// <param name="clientTimestampUtc">Device GPS-fix time (UTC) — drives the idempotent write guard.</param>
    public async Task PushLocation(int bookingUid, decimal latitude, decimal longitude, DateTime clientTimestampUtc)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("Unable to resolve caller identity.");
        }

        var providerUid = await _db.Providers
            .AsNoTracking()
            .Where(p => p.UserUid == userId)
            .Select(p => (int?)p.Uid)
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (providerUid == null)
        {
            throw new HubException("Caller is not a provider.");
        }

        var ownsBooking = await _db.ServiceBookings
            .AsNoTracking()
            .AnyAsync(b => b.Uid == bookingUid && b.ProviderUid == providerUid.Value, Context.ConnectionAborted);

        if (!ownsBooking)
        {
            throw new HubException("Provider does not own this booking.");
        }

        var (success, error, appliedAtUtc) = await _locationService.UpdateCurrentLocationAsync(
            providerUid.Value, latitude, longitude, clientTimestampUtc, Context.ConnectionAborted);

        if (!success)
        {
            throw new HubException(error ?? "Failed to update location.");
        }

        // Only broadcast when the write actually applied — a stale/duplicate retry must not push
        // a redundant LocationUpdated event to subscribed clients.
        if (appliedAtUtc is not null)
        {
            await Clients.Group($"booking-{bookingUid}")
                .SendAsync("LocationUpdated", new { bookingUid, latitude, longitude, timestamp = appliedAtUtc });
        }
    }

    /// <summary>Client (or admin/test-harness) subscribes to a specific booking's location stream.</summary>
    public async Task JoinBookingGroup(int bookingUid)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"booking-{bookingUid}");
    }

    public async Task LeaveBookingGroup(int bookingUid)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"booking-{bookingUid}");
    }
}

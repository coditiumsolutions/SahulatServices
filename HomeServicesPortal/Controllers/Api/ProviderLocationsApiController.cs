using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.Hubs;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/provider-locations")]
public class ProviderLocationsApiController : ControllerBase
{
    private readonly IProviderLocationQueryService _locationService;
    private readonly AppDbContext _db;
    private readonly IHubContext<LocationTrackingHub> _hubContext;

    public ProviderLocationsApiController(
        IProviderLocationQueryService locationService,
        AppDbContext db,
        IHubContext<LocationTrackingHub> hubContext)
    {
        _locationService = locationService;
        _db = db;
        _hubContext = hubContext;
    }

    /// <summary>Find available/verified providers near a point, sorted by distance (Haversine).</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NearbyProviderApiDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NearbyProviderApiDto>>>> Nearby(
        [FromQuery] NearbyProvidersRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<IReadOnlyList<NearbyProviderApiDto>>.Fail(message));
        }

        var providers = await _locationService.FindNearbyProvidersAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NearbyProviderApiDto>>.Ok(
            providers,
            providers.Count == 0 ? "No nearby providers found." : "Nearby providers fetched successfully."));
    }

    /// <summary>Point-to-point distance between a client address and a provider's current location.</summary>
    [HttpGet("distance")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Distance(
        [FromQuery] int clientAddressUid,
        [FromQuery] int providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error, distanceKm) = await _locationService.GetDistanceAsync(
            clientAddressUid, providerUid, cancellationToken);

        if (!success || distanceKm == null)
        {
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to compute distance."));
        }

        return Ok(ApiResponse<object>.Ok(new { distanceKm }, "Distance computed successfully."));
    }

    /// <summary>
    /// REST fallback for pushing a provider's current location, for clients that can't hold a
    /// persistent SignalR connection. Shares the same idempotent write as the Hub's PushLocation.
    /// </summary>
    [HttpPut("{providerUid:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateLocation(
        int providerUid,
        [FromBody] UpdateProviderLocationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<object>.Fail(message));
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var ownsProvider = await _db.Providers
            .AsNoTracking()
            .AnyAsync(p => p.Uid == providerUid && p.UserUid == userId, cancellationToken);

        if (!ownsProvider)
        {
            return Forbid();
        }

        var (success, error, appliedAtUtc) = await _locationService.UpdateCurrentLocationAsync(
            providerUid, request.Latitude, request.Longitude, request.ClientTimestampUtc, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to update location."));
        }

        var applied = appliedAtUtc != null;

        if (applied && request.BookingUid.HasValue)
        {
            await _hubContext.Clients.Group($"booking-{request.BookingUid.Value}")
                .SendAsync("LocationUpdated", new
                {
                    bookingUid = request.BookingUid.Value,
                    latitude = request.Latitude,
                    longitude = request.Longitude,
                    timestamp = appliedAtUtc
                }, cancellationToken);
        }

        return Ok(ApiResponse<object>.Ok(
            new { latitude = request.Latitude, longitude = request.Longitude, locationUpdatedOn = appliedAtUtc, applied },
            applied ? "Location updated successfully." : "Update ignored: a newer location is already stored."));
    }
}

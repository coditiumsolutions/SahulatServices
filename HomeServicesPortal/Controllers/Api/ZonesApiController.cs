using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

/// <summary>Service zones: the configured options, and a provider's own zones (Configurations key=Zone).</summary>
[ApiController]
[AllowAnonymous]
public class ZonesApiController : ControllerBase
{
    private readonly IProviderZoneService _zones;

    public ZonesApiController(IProviderZoneService zones)
    {
        _zones = zones;
    }

    /// <summary>Configured zone names, for the registration / profile zone picker.</summary>
    [HttpGet("api/zones")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetOptions(CancellationToken cancellationToken)
    {
        var options = await _zones.GetZoneOptionsAsync(cancellationToken);
        return Ok(ApiResponse<List<string>>.Ok(options, "Zones fetched successfully."));
    }

    /// <summary>A provider's zones (empty list if none).</summary>
    [HttpGet("api/providers/{providerUid:int}/zones")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetProviderZones(
        int providerUid, CancellationToken cancellationToken)
    {
        var zones = await _zones.GetZonesAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<List<string>>.Ok(zones, "Provider zones fetched successfully."));
    }

    /// <summary>Full-replace a provider's zones. Idempotent: the same list twice gives the same result.</summary>
    [HttpPut("api/providers/{providerUid:int}/zones")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<string>>>> UpdateProviderZones(
        int providerUid,
        [FromBody] UpdateProviderZonesRequestDto request,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _zones.UpdateZonesAsync(providerUid, request.Zones, cancellationToken);
        if (!success)
        {
            return BadRequest(ApiResponse<List<string>>.Fail(error ?? "Failed to update provider zones."));
        }

        var zones = await _zones.GetZonesAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<List<string>>.Ok(zones, "Provider zones updated successfully."));
    }
}

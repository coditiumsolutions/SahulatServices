using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/geocoding")]
[AllowAnonymous]
public class GeocodingApiController : ControllerBase
{
    private readonly INominatimService _nominatimService;

    public GeocodingApiController(INominatimService nominatimService)
    {
        _nominatimService = nominatimService;
    }

    /// <summary>Reverse-geocode a coordinate pair into a human-readable address via OpenStreetMap Nominatim.</summary>
    [HttpGet("reverse")]
    [ProducesResponseType(typeof(ApiResponse<ReverseGeocodeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReverseGeocodeResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ReverseGeocodeResultDto>>> Reverse(
        [FromQuery] ReverseGeocodeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ReverseGeocodeResultDto>.Fail(message));
        }

        var (success, error, data) = await _nominatimService.ReverseGeocodeAsync(
            request.Lat, request.Lng, cancellationToken);

        if (!success || data == null)
        {
            return BadRequest(ApiResponse<ReverseGeocodeResultDto>.Fail(error ?? "Failed to reverse geocode."));
        }

        return Ok(ApiResponse<ReverseGeocodeResultDto>.Ok(data, "Address resolved successfully."));
    }

    /// <summary>Forward-geocode a free-text query (e.g. "Gulberg Lahore") into candidate coordinate + address results via OpenStreetMap Nominatim.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<GeocodeSearchResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GeocodeSearchResultDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<GeocodeSearchResultDto>>>> Search(
        [FromQuery] GeocodeSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<List<GeocodeSearchResultDto>>.Fail(message));
        }

        var (success, error, data) = await _nominatimService.SearchGeocodeAsync(
            request.Q!, request.Limit, cancellationToken);

        if (!success || data == null)
        {
            return BadRequest(ApiResponse<List<GeocodeSearchResultDto>>.Fail(error ?? "Unable to search for the given query."));
        }

        return Ok(ApiResponse<List<GeocodeSearchResultDto>>.Ok(data, "Search results retrieved successfully."));
    }
}

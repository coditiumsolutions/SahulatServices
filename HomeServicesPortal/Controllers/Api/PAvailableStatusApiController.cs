using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/provider-avability-status")]
[AllowAnonymous]
public class ProviderAvailabilityStatusApiController : ControllerBase
{
    private readonly IProviderAvailabilityService _service;

    public ProviderAvailabilityStatusApiController(IProviderAvailabilityService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get provider online/offline status and timing from Providers table.
    /// </summary>
    [HttpGet("{providerUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderAvailableStatusApiDto>>> GetStatus(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data) = await _service.GetProviderAvailabilityStatusAsync(providerUid, cancellationToken);

        if (!success || data == null)
        {
            return NotFound(ApiResponse<ProviderAvailableStatusApiDto>.Fail(error ?? "Provider not found."));
        }

        return Ok(ApiResponse<ProviderAvailableStatusApiDto>.Ok(
            data,
            "Provider availability status fetched successfully."));
    }

    /// <summary>
    /// Create provider online/offline status and timing on Providers table.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderAvailableStatusApiDto>>> CreateStatus(
        [FromBody] SetProviderAvailableStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ProviderAvailableStatusApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.SaveProviderAvailabilityStatusAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<ProviderAvailableStatusApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<ProviderAvailableStatusApiDto>.Fail(error ?? "Failed to update status."));
        }

        return Ok(ApiResponse<ProviderAvailableStatusApiDto>.Ok(
            data,
            "Provider availability status saved successfully."));
    }

    /// <summary>
    /// Edit provider online/offline status and timing on Providers table.
    /// </summary>
    [HttpPut("{providerUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProviderAvailableStatusApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderAvailableStatusApiDto>>> EditStatus(
        int providerUid,
        [FromBody] SetProviderAvailableStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        if (providerUid != request.ProviderUid)
        {
            return BadRequest(ApiResponse<ProviderAvailableStatusApiDto>.Fail("ProviderUid in URL and body must match."));
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ProviderAvailableStatusApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.SaveProviderAvailabilityStatusAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<ProviderAvailableStatusApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<ProviderAvailableStatusApiDto>.Fail(error ?? "Failed to update status."));
        }

        return Ok(ApiResponse<ProviderAvailableStatusApiDto>.Ok(
            data,
            "Provider availability status updated successfully."));
    }
}

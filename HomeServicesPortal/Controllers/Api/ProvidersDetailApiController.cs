using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/providers-detail")]
[AllowAnonymous]
public class ProvidersDetailApiController : ControllerBase
{
    private readonly IProviderDetailService _service;

    public ProvidersDetailApiController(IProviderDetailService service)
    {
        _service = service;
    }

    /// <summary>Fetch provider detail from Providers table.</summary>
    [HttpGet("{providerUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDetailApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDetailApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDetailApiDto>>> GetDetail(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data) = await _service.GetProviderDetailAsync(providerUid, cancellationToken);

        if (!success || data == null)
        {
            return NotFound(ApiResponse<ProviderDetailApiDto>.Fail(error ?? "Provider not found."));
        }

        return Ok(ApiResponse<ProviderDetailApiDto>.Ok(data, "Provider detail fetched successfully."));
    }

    /// <summary>Edit provider detail in Providers table.</summary>
    [HttpPut("{providerUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDetailApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDetailApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDetailApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDetailApiDto>>> EditDetail(
        int providerUid,
        [FromBody] UpdateProviderDetailRequestDto request,
        CancellationToken cancellationToken)
    {
        if (providerUid != request.ProviderUid)
        {
            return BadRequest(ApiResponse<ProviderDetailApiDto>.Fail("ProviderUid in URL and body must match."));
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ProviderDetailApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.UpdateProviderDetailAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<ProviderDetailApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<ProviderDetailApiDto>.Fail(error ?? "Failed to update provider detail."));
        }

        return Ok(ApiResponse<ProviderDetailApiDto>.Ok(data, "Provider detail updated successfully."));
    }
}

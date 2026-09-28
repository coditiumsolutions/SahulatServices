using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

/// <summary>
/// Manage a provider's optional service-title membership (ProviderServiceTitles junction table),
/// scoped to categories the provider already has via ProviderCategories. Additive, non-breaking —
/// does not change any existing endpoint's contract.
/// </summary>
[ApiController]
[Route("api/providers/{providerUid:int}/service-titles")]
[AllowAnonymous]
public class ProviderServiceTitlesApiController : ControllerBase
{
    private readonly IProviderServiceTitleService _service;

    public ProviderServiceTitlesApiController(IProviderServiceTitleService service)
    {
        _service = service;
    }

    /// <summary>List a provider's service titles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderServiceTitleItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ProviderServiceTitleItemDto>>>> GetServiceTitles(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var titles = await _service.GetServiceTitlesAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<List<ProviderServiceTitleItemDto>>.Ok(titles, "Provider service titles fetched successfully."));
    }

    /// <summary>Full-replace a provider's service-title set. An empty list is valid and removes all titles.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderServiceTitleItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderServiceTitleItemDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<ProviderServiceTitleItemDto>>>> UpdateServiceTitles(
        int providerUid,
        [FromBody] UpdateProviderServiceTitlesRequestDto request,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _service.SyncServiceTitlesAsync(
            providerUid, request.ServiceTitleIds, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<List<ProviderServiceTitleItemDto>>.Fail(error ?? "Failed to update provider service titles."));
        }

        var titles = await _service.GetServiceTitlesAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<List<ProviderServiceTitleItemDto>>.Ok(titles, "Provider service titles updated successfully."));
    }
}

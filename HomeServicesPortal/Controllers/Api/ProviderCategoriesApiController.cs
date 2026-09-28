using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

/// <summary>
/// Manage a provider's multi-category membership (ProviderCategories junction table). Additive,
/// non-breaking endpoints — does not change the registration contract (POST /api/auth/register-provider
/// remains single-category pending approval; see docs/flutter-changes.md).
/// </summary>
[ApiController]
[Route("api/providers/{providerUid:int}/categories")]
[AllowAnonymous]
public class ProviderCategoriesApiController : ControllerBase
{
    private readonly IProviderCategoryService _service;

    public ProviderCategoriesApiController(IProviderCategoryService service)
    {
        _service = service;
    }

    /// <summary>List a provider's categories, flagging which one is primary.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderCategoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ProviderCategoryItemDto>>>> GetCategories(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var categories = await _service.GetCategoriesAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<List<ProviderCategoryItemDto>>.Ok(categories, "Provider categories fetched successfully."));
    }

    /// <summary>Full-replace a provider's category set.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderCategoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderCategoryItemDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<ProviderCategoryItemDto>>>> UpdateCategories(
        int providerUid,
        [FromBody] UpdateProviderCategoriesRequestDto request,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _service.SyncCategoriesAsync(
            providerUid, request.CategoryIds, request.PrimaryCategoryId, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<List<ProviderCategoryItemDto>>.Fail(error ?? "Failed to update provider categories."));
        }

        var categories = await _service.GetCategoriesAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<List<ProviderCategoryItemDto>>.Ok(categories, "Provider categories updated successfully."));
    }
}

using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/provider-profiles")]
[AllowAnonymous]
public class ProviderProfilesApiController : ControllerBase
{
    private readonly IServiceProviderService _service;

    public ProviderProfilesApiController(IServiceProviderService service)
    {
        _service = service;
    }

    /// <summary>Get all active provider profiles, optionally filtered by category.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderProfileApiDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProviderProfileApiDto>>>> GetAll(
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var profiles = await _service.GetProviderProfilesForApiAsync(categoryId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProviderProfileApiDto>>.Ok(
            profiles,
            profiles.Count == 0 ? "No provider profiles found." : "Provider profiles fetched successfully."));
    }

    /// <summary>Get provider profile by Users.UID (UserUID).</summary>
    [HttpGet("{userUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderProfileApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderProfileApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderProfileApiDto>>> GetByUserUid(
        int userUid,
        CancellationToken cancellationToken)
    {
        var profile = await _service.GetProviderProfileByUserUidAsync(userUid, cancellationToken);
        if (profile == null)
        {
            return NotFound(ApiResponse<ProviderProfileApiDto>.Fail("Provider profile not found for this user."));
        }

        return Ok(ApiResponse<ProviderProfileApiDto>.Ok(profile, "Provider profile fetched successfully."));
    }
}

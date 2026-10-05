using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HomeServicesPortal.Services;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/v1/app")]
[AllowAnonymous]
public class AppConfigApiController : ControllerBase
{
    private readonly IAppVersionPolicyService _policies;

    public AppConfigApiController(IAppVersionPolicyService policies)
    {
        _policies = policies;
    }

    /// <summary>
    /// App version / force-update policy for a platform. Values come from appsettings (AppConfig); LatestVersion can be
    /// overridden by a value saved from the admin portal (see AppVersionPolicyService).
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(AppConfigApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConfig([FromQuery] string? platform, CancellationToken cancellationToken)
    {
        var config = await _policies.GetEffectiveAsync(cancellationToken);
        var policy = platform?.Trim().ToLowerInvariant() switch
        {
            "android" => config.Android,
            "ios" => config.Ios,
            _ => null
        };

        if (policy == null)
        {
            return BadRequest(ApiResponse<object>.Fail("platform must be 'android' or 'ios'."));
        }

        return Ok(new AppConfigApiDto
        {
            MinimumRequiredVersion = policy.MinimumRequiredVersion,
            LatestVersion = policy.LatestVersion,
            ForceUpdate = policy.ForceUpdate,
            StoreUrl = policy.StoreUrl,
            UpdateMessage = policy.UpdateMessage
        });
    }
}

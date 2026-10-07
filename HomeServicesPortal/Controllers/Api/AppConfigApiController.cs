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
    private readonly IUpdateBlockReleaseService _releases;

    public AppConfigApiController(IAppVersionPolicyService policies, IUpdateBlockReleaseService releases)
    {
        _policies = policies;
        _releases = releases;
    }

    /// <summary>
    /// App version / force-update policy for a platform. Values come from appsettings (AppConfig); LatestVersion can be
    /// overridden by a value saved from the admin portal (see AppVersionPolicyService).
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(AppConfigApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConfig([FromQuery] string? platform,
        [FromQuery(Name = "device_token")] string? deviceToken, CancellationToken cancellationToken)
    {
        // A release must be seen on the next call, and device_token must never sit in a shared cache.
        Response.Headers.CacheControl = "no-store";

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

        // device_token is only used to look up user/device-scoped releases: never stored, never logged. A token over
        // the column size (512) cannot be registered, so it is treated as no token.
        var normalizedPlatform = platform!.Trim().ToLowerInvariant();
        var token = deviceToken is { Length: <= 512 } ? deviceToken : null;
        var lastUnblock = await _releases.GetLastReleaseAtAsync(normalizedPlatform, token, cancellationToken);

        return Ok(new AppConfigApiDto
        {
            MinimumRequiredVersion = policy.MinimumRequiredVersion,
            LatestVersion = policy.LatestVersion,
            ForceUpdate = policy.ForceUpdate,
            StoreUrl = policy.StoreUrl,
            UpdateMessage = policy.UpdateMessage,
            LastUnblockAt = lastUnblock?.ToString("O")
        });
    }
}

using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/v1/app")]
[AllowAnonymous]
public class AppConfigApiController : ControllerBase
{
    private readonly IOptionsMonitor<AppConfigOptions> _options;

    public AppConfigApiController(IOptionsMonitor<AppConfigOptions> options)
    {
        _options = options;
    }

    /// <summary>App version / force-update policy for a platform. Values come from appsettings (AppConfig).</summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(AppConfigApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public IActionResult GetConfig([FromQuery] string? platform)
    {
        var config = _options.CurrentValue;
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

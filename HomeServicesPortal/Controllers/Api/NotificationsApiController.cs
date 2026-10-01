using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsApiController : ControllerBase
{
    private static readonly string[] Platforms = { "android", "ios" };

    private readonly INotificationService _notifications;

    public NotificationsApiController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>Register (or refresh) the caller's FCM device token. Idempotent.</summary>
    [HttpPost("register-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> RegisterToken(
        [FromBody] RegisterDeviceTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var userType = request.UserType.Trim();
        if (userType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase))
        {
            userType = UserTypeConstants.Client;
        }
        else if (userType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            userType = UserTypeConstants.Provider;
        }
        else
        {
            return BadRequest(ApiResponse<object>.Fail("userType must be 'Client' or 'Provider'."));
        }

        var platform = request.Platform.Trim().ToLowerInvariant();
        if (!Platforms.Contains(platform))
        {
            return BadRequest(ApiResponse<object>.Fail("platform must be 'android' or 'ios'."));
        }

        // A caller may only register tokens for themselves.
        var tokenUserType = User.FindFirst("UserType")?.Value;
        if (!int.TryParse(User.FindFirst("UserId")?.Value, out var authUserId) ||
            authUserId != request.UserId ||
            !string.Equals(tokenUserType, userType, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("userId/userType do not match the authenticated user."));
        }

        await _notifications.UpsertDeviceTokenAsync(
            request.UserId, userType, request.DeviceToken.Trim(), platform, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Device token registered successfully."));
    }
}

using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

// TODO(auth): like the rest of the API this is anonymous for now (identity comes from the body/query).
// When endpoints are secured, switch to [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
// and require userId / userType to match the "UserId" / "UserType" claims from JwtTokenService.CreateToken
// (403 otherwise), read the user from the claims for the inbox endpoints, and scope unregister-token to the
// caller's own rows. Without that anyone who knows a userId can register a device under it and read that
// user's inbox. See docs/auth-gap-report.md finding 9 for the full checklist.
[ApiController]
[Route("api/notifications")]
[AllowAnonymous]
public class NotificationsApiController : ControllerBase
{
    private static readonly string[] Platforms = { "android", "ios" };

    private readonly INotificationService _notifications;

    public NotificationsApiController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>Register (or refresh) a device's FCM token for a user. Idempotent.</summary>
    [HttpPost("register-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
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

        if (!await _notifications.UserExistsAsync(request.UserId, userType, cancellationToken))
        {
            return BadRequest(ApiResponse<object>.Fail("Unknown or inactive user."));
        }

        await _notifications.UpsertDeviceTokenAsync(
            request.UserId, userType, request.DeviceToken.Trim(), platform, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Device token registered successfully."));
    }

    /// <summary>The user's in-app inbox, newest first. userType (Client/Provider) optionally limits it to one role.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserNotificationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserNotificationListDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserNotificationListDto>>> GetInbox(
        [FromQuery] int userId,
        [FromQuery] string? userType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return BadRequest(ApiResponse<UserNotificationListDto>.Fail("userId is required."));
        }

        if (!TryNormalizeRole(userType, out var role))
        {
            return BadRequest(ApiResponse<UserNotificationListDto>.Fail("userType must be 'Client' or 'Provider'."));
        }

        var data = await _notifications.GetInboxAsync(userId, role, page, pageSize, cancellationToken);
        return Ok(ApiResponse<UserNotificationListDto>.Ok(data, "Notifications fetched successfully."));
    }

    /// <summary>Number of unread inbox notifications (for the app's badge).</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountApiDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UnreadCountApiDto>>> GetUnreadCount(
        [FromQuery] int userId,
        [FromQuery] string? userType,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return BadRequest(ApiResponse<UnreadCountApiDto>.Fail("userId is required."));
        }

        if (!TryNormalizeRole(userType, out var role))
        {
            return BadRequest(ApiResponse<UnreadCountApiDto>.Fail("userType must be 'Client' or 'Provider'."));
        }

        var count = await _notifications.GetUnreadCountAsync(userId, role, cancellationToken);
        return Ok(ApiResponse<UnreadCountApiDto>.Ok(new UnreadCountApiDto { UnreadCount = count }, "Unread count fetched successfully."));
    }

    /// <summary>Mark one notification read. Idempotent; 404 if it doesn't exist or belongs to another user.</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(
        int id,
        [FromBody] MarkNotificationReadRequestDto request,
        CancellationToken cancellationToken)
    {
        var found = await _notifications.MarkReadAsync(id, request.UserId, cancellationToken);
        return found
            ? Ok(ApiResponse<object>.Ok(new { }, "Notification marked as read."))
            : NotFound(ApiResponse<object>.Fail("Notification not found."));
    }

    /// <summary>Mark every unread notification read (optionally only one role's). Idempotent.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(
        [FromBody] MarkAllNotificationsReadRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeRole(request.UserType, out var role))
        {
            return BadRequest(ApiResponse<object>.Fail("userType must be 'Client' or 'Provider'."));
        }

        var updated = await _notifications.MarkAllReadAsync(request.UserId, role, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { updated }, "Notifications marked as read."));
    }

    /// <summary>Null/blank means "no role filter" (valid); otherwise Client or Provider, case-insensitive.</summary>
    private static bool TryNormalizeRole(string? userType, out string? role)
    {
        role = null;
        if (string.IsNullOrWhiteSpace(userType)) return true;

        if (userType.Trim().Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase))
            role = UserTypeConstants.Client;
        else if (userType.Trim().Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
            role = UserTypeConstants.Provider;
        else
            return false;

        return true;
    }

    /// <summary>Remove a device's FCM token (call on logout). Idempotent.</summary>
    [HttpPost("unregister-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> UnregisterToken(
        [FromBody] UnregisterDeviceTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        await _notifications.RemoveDeviceTokenAsync(request.DeviceToken.Trim(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Device token removed."));
    }
}

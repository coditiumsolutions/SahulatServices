using System.Security.Claims;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>Notification inbox and bell for the signed-in client or provider. The inbox follows the account's role.</summary>
[Authorize(Roles = UserTypeConstants.Client + "," + UserTypeConstants.Provider)]
public class NotificationsPortalController : Controller
{
    private const int PageSize = 20;

    private readonly INotificationService _notifications;
    private readonly ILogger<NotificationsPortalController> _logger;

    public NotificationsPortalController(INotificationService notifications, ILogger<NotificationsPortalController> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    [HttpGet("/client/notifications")]
    [HttpGet("/provider/notifications")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!TryGetUser(out var userId, out var role))
        {
            return Redirect("/login");
        }

        if (!Request.Path.StartsWithSegments(HomePath(role)))
        {
            return Redirect(HomePath(role) + "/notifications");
        }

        page = Math.Max(1, page);
        var data = await _notifications.GetInboxAsync(userId, role, page, PageSize, cancellationToken);

        ViewData["Title"] = "Notifications";
        ViewData["Role"] = role;
        return View("Notifications", data);
    }

    /// <summary>Unread badge count, polled by the bell.</summary>
    [HttpGet("/client/notifications/count")]
    [HttpGet("/provider/notifications/count")]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        if (!TryGetUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        Response.Headers.CacheControl = "no-store";
        var unread = await _notifications.GetUnreadCountAsync(userId, role, cancellationToken);
        return Json(new { unread });
    }

    /// <summary>Mark one notification read, then go to what it is about (or back to the inbox).</summary>
    [HttpPost("/client/notifications/{id:int}/open")]
    [HttpPost("/provider/notifications/{id:int}/open")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id, string? next, int page = 1, CancellationToken cancellationToken = default)
    {
        if (!TryGetUser(out var userId, out var role))
        {
            return Redirect("/login");
        }

        // Idempotent; a notification that is not this user's is simply not found and ignored.
        await _notifications.MarkReadAsync(id, userId, cancellationToken);

        // The link was built for this role when the page rendered; still, never follow anything off-site.
        if (NotificationDisplayHelper.IsSafeTarget(next) && next!.StartsWith(HomePath(role) + "/", StringComparison.Ordinal))
        {
            return Redirect(next);
        }

        return Redirect($"{HomePath(role)}/notifications?page={Math.Max(1, page)}");
    }

    [HttpPost("/client/notifications/read-all")]
    [HttpPost("/provider/notifications/read-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll(CancellationToken cancellationToken)
    {
        if (!TryGetUser(out var userId, out var role))
        {
            return Redirect("/login");
        }

        await _notifications.MarkAllReadAsync(userId, role, cancellationToken);
        return Redirect(HomePath(role) + "/notifications");
    }

    private bool TryGetUser(out int userId, out string role)
    {
        role = User.FindFirstValue("UserType") ?? string.Empty;
        var ok = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        if (role.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            role = UserTypeConstants.Provider;
        }
        else if (role.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase))
        {
            role = UserTypeConstants.Client;
        }
        else
        {
            ok = false;
        }

        return ok;
    }

    private static string HomePath(string role) => role == UserTypeConstants.Provider ? "/provider" : "/client";
}

using System.Text.Json;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
[Route("Admin/Notifications")]
public class AdminNotificationsController : Controller
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAdminNotificationService _service;

    public AdminNotificationsController(IAdminNotificationService service)
    {
        _service = service;
    }

    [HttpGet("feed")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Feed(CancellationToken cancellationToken)
    {
        var feed = await _service.GetRecentAsync(cancellationToken: cancellationToken);
        return new JsonResult(feed, JsonOpts);
    }

    [HttpGet("unread-count")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var feed = await _service.GetRecentAsync(take: 1, cancellationToken: cancellationToken);
        return new JsonResult(new { unreadCount = feed.UnreadCount }, JsonOpts);
    }

    [HttpPost("mark-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(CancellationToken cancellationToken)
    {
        var marked = await _service.MarkAllReadAsync(cancellationToken);
        return new JsonResult(new { success = true, marked, unreadCount = 0 }, JsonOpts);
    }
}

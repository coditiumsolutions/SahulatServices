using FirebaseAdmin;
using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Staff-sent push to every registered device (e.g. "new app version available"). Deliberately a separate
/// route from /Admin/Notifications, which is the admin bell feed (AdminNotificationsController).
/// </summary>
[Authorize(Roles = "Super Admin,Admin")]
public class PushBroadcastController : Controller
{
    private static readonly string[] Platforms = { "all", "android", "ios" };

    private readonly INotificationService _notifications;
    private readonly AppDbContext _db;
    private readonly ILogger<PushBroadcastController> _logger;

    public PushBroadcastController(INotificationService notifications, AppDbContext db, ILogger<PushBroadcastController> logger)
    {
        _notifications = notifications;
        _db = db;
        _logger = logger;
    }

    [HttpGet("/Admin/PushBroadcast")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = new PushBroadcastFormVm();
        await FillReachAsync(vm, cancellationToken);
        return View(vm);
    }

    [HttpPost("/Admin/PushBroadcast")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(PushBroadcastFormVm model, CancellationToken cancellationToken)
    {
        model.Platform = (model.Platform ?? string.Empty).Trim().ToLowerInvariant();
        if (!Platforms.Contains(model.Platform))
        {
            ModelState.AddModelError(nameof(model.Platform), "Choose All, Android or iOS.");
        }

        if (!ModelState.IsValid)
        {
            await FillReachAsync(model, cancellationToken);
            return View("Index", model);
        }

        var data = new Dictionary<string, string>
        {
            ["type"] = NotificationTypes.AppUpdate,
            ["screen"] = NotificationScreens.AppUpdate
        };

        var result = await _notifications.SendBroadcastAsync(
            model.Platform, model.Title.Trim(), model.Message.Trim(), data, cancellationToken);

        _logger.LogInformation(
            "Push broadcast by {User}: platform={Platform}, recipients={Recipients}, sent={Sent}, failed={Failed}, removedStale={Removed}.",
            User.Identity?.Name, model.Platform, result.Recipients, result.Sent, result.Failed, result.RemovedStale);

        if (!result.FirebaseConfigured)
        {
            TempData["ErrorMessage"] = "Push is not configured on this server (Firebase service account missing), so nothing was sent.";
        }
        else if (result.Recipients == 0)
        {
            TempData["ErrorMessage"] = "No registered devices for that platform yet, so nothing was sent.";
        }
        else
        {
            TempData["SuccessMessage"] =
                $"Broadcast sent to {result.Recipients} device(s): {result.Sent} delivered to FCM, {result.Failed} failed" +
                (result.RemovedStale > 0 ? $", {result.RemovedStale} expired token(s) removed." : ".");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task FillReachAsync(PushBroadcastFormVm vm, CancellationToken cancellationToken)
    {
        vm.FirebaseConfigured = FirebaseApp.DefaultInstance != null;
        vm.AndroidDevices = await _db.UserDeviceTokens.AsNoTracking().CountAsync(t => t.Platform == "android", cancellationToken);
        vm.IosDevices = await _db.UserDeviceTokens.AsNoTracking().CountAsync(t => t.Platform == "ios", cancellationToken);
    }
}

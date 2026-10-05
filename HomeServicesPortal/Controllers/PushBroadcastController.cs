using FirebaseAdmin;
using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Options;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    private readonly IAppVersionPolicyService _policies;
    private readonly IStoreVersionChecker _storeChecker;
    private readonly ILogger<PushBroadcastController> _logger;

    public PushBroadcastController(INotificationService notifications, AppDbContext db,
        IAppVersionPolicyService policies, IStoreVersionChecker storeChecker, ILogger<PushBroadcastController> logger)
    {
        _notifications = notifications;
        _db = db;
        _policies = policies;
        _storeChecker = storeChecker;
        _logger = logger;
    }

    [HttpGet("/Admin/PushBroadcast")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = new PushBroadcastFormVm();
        await FillReachAsync(vm, cancellationToken);
        var effective = await _policies.GetEffectiveAsync(cancellationToken);
        vm.AndroidLatestVersion = effective.Android.LatestVersion;
        vm.IosLatestVersion = effective.Ios.LatestVersion;
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

        // One payload per targeted platform: each carries that platform's own latest_version / store_url. A platform
        // with no usable version rejects the whole broadcast, so nothing is sent for either platform.
        var targets = new List<(string Platform, Dictionary<string, string> Data)>();
        if (ModelState.IsValid)
        {
            var config = await _policies.GetEffectiveAsync(cancellationToken);
            var wanted = model.Platform == "all" ? AppUpdatePayload.Platforms : new[] { model.Platform };
            foreach (var platform in wanted)
            {
                var typed = platform == "ios" ? model.IosLatestVersion : model.AndroidLatestVersion;
                if (AppUpdatePayload.TryBuild(config, platform, out var payload, out var error, typed ?? string.Empty,
                        platform == "ios" ? model.IosForceUpdate : model.AndroidForceUpdate))
                    targets.Add((platform, payload));
                else
                    ModelState.AddModelError(string.Empty, error!);
            }
        }

        if (!ModelState.IsValid)
        {
            await FillReachAsync(model, cancellationToken);
            return View("Index", model);
        }

        // Separate sends per platform (never one mixed payload), totals added up for the admin message.
        var result = new BroadcastResult(true, 0, 0, 0, 0);
        foreach (var (platform, data) in targets)
        {
            // Always on the announcements channel (announcements_v2, whose sound is confirmed in the app), whatever
            // Notifications:AndroidChannelsEnabled says for booking pushes.
            var part = await _notifications.SendBroadcastAsync(
                platform, model.Title.Trim(), model.Message.Trim(), data, androidChannels: true, cancellationToken: cancellationToken);
            result = new BroadcastResult(
                result.FirebaseConfigured && part.FirebaseConfigured,
                result.Recipients + part.Recipients, result.Sent + part.Sent,
                result.Failed + part.Failed, result.RemovedStale + part.RemovedStale);
        }

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

    /// <summary>
    /// "Check store for update": reads the version published in that platform's store listing (the store URL from
    /// AppConfig) and saves it as the platform's latest version, so the app-config endpoint and the next push agree.
    /// Returns JSON for the form's button; saves nothing when the store can't be read or returns something unusable.
    /// </summary>
    [HttpPost("/Admin/PushBroadcast/CheckStore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckStore(string platform, CancellationToken cancellationToken)
    {
        platform = (platform ?? string.Empty).Trim().ToLowerInvariant();
        if (!AppUpdatePayload.Platforms.Contains(platform))
            return BadRequest(new { success = false, message = "Unknown platform." });

        var effective = await _policies.GetEffectiveAsync(cancellationToken);
        var policy = AppUpdatePayload.PolicyFor(effective, platform);
        var label = platform == "ios" ? "App Store" : "Google Play";
        if (string.IsNullOrWhiteSpace(policy.StoreUrl))
            return Ok(new { success = false, message = $"No {label} URL is set in AppConfig, so there is nothing to check." });

        var result = await _storeChecker.CheckAsync(platform, policy.StoreUrl, cancellationToken);
        if (!result.Success)
            return Ok(new { success = false, message = result.Error });

        var previous = policy.LatestVersion;
        await _policies.SaveLatestVersionAsync(platform, result.Version!, cancellationToken);
        _logger.LogInformation("Store check by {User}: {Platform} store lists {Raw}, saved latest_version {Version} (was {Previous}).",
            User.Identity?.Name, platform, result.RawVersion, result.Version, previous);

        var note = result.RawVersion != result.Version ? $" (store shows \"{result.RawVersion}\")" : string.Empty;
        return Ok(new
        {
            success = true,
            version = result.Version,
            message = $"{label} lists {result.Version}{note}. Saved as the latest version (was {previous})."
        });
    }

    private async Task FillReachAsync(PushBroadcastFormVm vm, CancellationToken cancellationToken)
    {
        vm.FirebaseConfigured = FirebaseApp.DefaultInstance != null;
        vm.AndroidDevices = await _db.UserDeviceTokens.AsNoTracking().CountAsync(t => t.Platform == "android", cancellationToken);
        vm.IosDevices = await _db.UserDeviceTokens.AsNoTracking().CountAsync(t => t.Platform == "ios", cancellationToken);

        // Store links come from AppConfig (display only). The version fields keep what staff typed.
        var config = await _policies.GetEffectiveAsync(cancellationToken);
        vm.AndroidStoreUrl = config.Android.StoreUrl;
        vm.IosStoreUrl = config.Ios.StoreUrl;
    }
}

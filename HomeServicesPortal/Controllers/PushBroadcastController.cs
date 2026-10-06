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
    private readonly IUpdateBlockReleaseService _releases;
    private readonly ILogger<PushBroadcastController> _logger;

    public PushBroadcastController(INotificationService notifications, AppDbContext db,
        IAppVersionPolicyService policies, IStoreVersionChecker storeChecker, IUpdateBlockReleaseService releases,
        ILogger<PushBroadcastController> logger)
    {
        _releases = releases;
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
        vm.ReleaseMessage = TempData["ReleaseMessage"] as string;
        vm.ReleaseError = TempData["ReleaseError"] as string;
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

    /// <summary>
    /// Reads the version the store currently lists WITHOUT saving it. Used by the form's "forced version is ahead of the
    /// store" warning, so checking never changes the live version.
    /// </summary>
    [HttpPost("/Admin/PushBroadcast/PeekStore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PeekStore(string platform, CancellationToken cancellationToken)
    {
        platform = (platform ?? string.Empty).Trim().ToLowerInvariant();
        if (!AppUpdatePayload.Platforms.Contains(platform))
            return BadRequest(new { success = false, message = "Unknown platform." });

        var policy = AppUpdatePayload.PolicyFor(await _policies.GetEffectiveAsync(cancellationToken), platform);
        if (string.IsNullOrWhiteSpace(policy.StoreUrl))
            return Ok(new { success = false, version = (string?)null });

        var result = await _storeChecker.CheckAsync(platform, policy.StoreUrl, cancellationToken);
        return Ok(new { success = result.Success, version = result.Version });
    }

    /// <summary>How many registered devices a release scope would reach (for the confirm step). Sends nothing.</summary>
    [HttpPost("/Admin/PushBroadcast/ReleaseReach")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseReach(ReleaseFormVm model, CancellationToken cancellationToken)
    {
        var (devices, error) = await _releases.ResolveAsync(
            new ReleaseTarget(NormalizeScope(model.Scope), null, model.UserId, model.TokenId), cancellationToken);
        return Ok(new { count = devices.Count, error });
    }

    /// <summary>
    /// Admin release of update blocks: sends the silent app_unblock push to the chosen scope and records it. Admin
    /// portal only (there is deliberately no mobile endpoint), anti-forgery protected, logged with the admin username.
    /// </summary>
    [HttpPost("/Admin/PushBroadcast/Release")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(ReleaseFormVm model, CancellationToken cancellationToken)
    {
        var scope = NormalizeScope(model.Scope);
        var platform = scope is "android" or "ios" ? scope : null;
        var outcome = await _releases.ReleaseAsync(new ReleaseTarget(scope, platform, model.UserId, model.TokenId),
            model.Reason, User.Identity?.Name ?? "unknown", cancellationToken);

        if (outcome.Error != null)
        {
            TempData["ReleaseError"] = outcome.Error;
        }
        else
        {
            var r = outcome.Result!;
            TempData["ReleaseMessage"] =
                $"Release sent to {r.Recipients} device(s): {r.Sent} delivered to FCM, {r.Failed} failed" +
                (r.RemovedStale > 0 ? $", {r.RemovedStale} expired token(s) removed." : ".") +
                " iPhones may delay or drop silent pushes, so iOS delivery is best effort: send again if a device is still blocked.";
        }

        return Redirect("/Admin/PushBroadcast#release");
    }

    private static string NormalizeScope(string? scope) => (scope ?? string.Empty).Trim().ToLowerInvariant();

    private async Task FillReachAsync(PushBroadcastFormVm vm, CancellationToken cancellationToken)
    {
        vm.FirebaseConfigured = FirebaseApp.DefaultInstance != null;
        vm.AndroidDevices = await _db.UserDeviceTokens.AsNoTracking().CountAsync(t => t.Platform == "android", cancellationToken);
        vm.IosDevices = await _db.UserDeviceTokens.AsNoTracking().CountAsync(t => t.Platform == "ios", cancellationToken);

        // Store links come from AppConfig (display only). The version fields keep what staff typed.
        var config = await _policies.GetEffectiveAsync(cancellationToken);
        vm.AndroidStoreUrl = config.Android.StoreUrl;
        vm.IosStoreUrl = config.Ios.StoreUrl;
        vm.AndroidSavedVersion = config.Android.LatestVersion;
        vm.IosSavedVersion = config.Ios.LatestVersion;

        // "Release blocked devices" card: device picker (latest 200) and the append-only history (latest 50).
        var tokens = await _db.UserDeviceTokens.AsNoTracking()
            .OrderByDescending(t => t.UpdatedAt).Take(200)
            .Select(t => new PushTesterDeviceRow
            {
                Id = t.Id, UserId = t.UserId, UserType = t.UserType, Platform = t.Platform, UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        var userIds = tokens.Select(t => t.UserId).Distinct().ToList();
        var clients = await _db.Clients.AsNoTracking().Where(c => userIds.Contains(c.UserUid))
            .Select(c => new { c.UserUid, c.FullName }).ToListAsync(cancellationToken);
        var providers = await _db.Providers.AsNoTracking().Where(p => userIds.Contains(p.UserUid))
            .Select(p => new { p.UserUid, p.FullName }).ToListAsync(cancellationToken);
        foreach (var t in tokens)
        {
            t.Name = t.UserType == UserTypeConstants.Client
                ? clients.FirstOrDefault(c => c.UserUid == t.UserId)?.FullName
                : providers.FirstOrDefault(p => p.UserUid == t.UserId)?.FullName;
        }
        vm.ReleaseDevices = tokens;

        var history = await _db.UpdateBlockReleases.AsNoTracking()
            .OrderByDescending(r => r.ReleasedAtUtc).ThenByDescending(r => r.Id).Take(50)
            .ToListAsync(cancellationToken);
        vm.ReleaseHistory = history.Select(r => new ReleaseHistoryRow
        {
            ReleasedAtUtc = r.ReleasedAtUtc,
            Scope = r.Scope,
            Target = r.Scope switch
            {
                "user" => $"user {r.UserId}",
                "device" => $"device #{r.DeviceTokenId}",
                "android" => "all Android",
                "ios" => "all iOS",
                _ => "everyone"
            },
            Reason = r.Reason,
            ReleasedBy = r.ReleasedBy,
            Recipients = r.Recipients,
            Sent = r.Sent,
            Failed = r.Failed
        }).ToList();
    }
}

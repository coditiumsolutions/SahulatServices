using FirebaseAdmin;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
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
/// Staff test tool: fire any booking notification (or a custom one) at one user, one device, or every Client/Provider
/// device. Real sends to real devices, so it is limited to admins and role-wide sends ask for confirmation in the view.
/// </summary>
[Authorize(Roles = "Super Admin,Admin")]
public class PushTesterController : Controller
{
    private static readonly string[] Platforms = { "all", "android", "ios" };
    private static readonly string[] Roles = { UserTypeConstants.Client, UserTypeConstants.Provider };

    private readonly INotificationService _notifications;
    private readonly AppDbContext _db;
    private readonly ILogger<PushTesterController> _logger;
    private readonly IOptionsMonitor<NotificationOptions> _channelsSetting;
    private readonly IAppVersionPolicyService _policies;

    public PushTesterController(INotificationService notifications, AppDbContext db, ILogger<PushTesterController> logger,
        IOptionsMonitor<NotificationOptions> channelsSetting, IAppVersionPolicyService policies)
    {
        _notifications = notifications;
        _db = db;
        _logger = logger;
        _channelsSetting = channelsSetting;
        _policies = policies;
    }

    [HttpGet("/Admin/PushTester")]
    public async Task<IActionResult> Index(int? userId, int? tokenId, CancellationToken cancellationToken)
    {
        var vm = new PushTesterFormVm { UserId = userId, TokenId = tokenId };
        if (tokenId != null) vm.TargetMode = "device";
        await FillAsync(vm, cancellationToken);
        return View(vm);
    }

    [HttpPost("/Admin/PushTester")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(PushTesterFormVm model, CancellationToken cancellationToken)
    {
        await FillAsync(model, cancellationToken);
        model.Result = null;

        var isCustom = model.EventKey == NotificationTestCatalog.CustomKey;
        var entry = isCustom ? null : NotificationTestCatalog.Find(model.EventKey);
        if (!isCustom && entry == null)
            ModelState.AddModelError(nameof(model.EventKey), "Choose a notification type.");
        if (isCustom && (string.IsNullOrWhiteSpace(model.CustomTitle) || string.IsNullOrWhiteSpace(model.CustomBody)))
            ModelState.AddModelError(nameof(model.CustomTitle), "A custom notification needs a title and a message.");
        if (!Roles.Contains(model.Role)) model.Role = UserTypeConstants.Client;
        model.Platform = (model.Platform ?? "all").Trim().ToLowerInvariant();
        if (!Platforms.Contains(model.Platform)) model.Platform = "all";

        // Role: events that the real flow sends to one role keep it; custom / app_update use the form's choice.
        var role = !string.IsNullOrEmpty(entry?.Role) ? entry!.Role : model.Role;

        // Resolve the target to (tokens, inbox user).
        List<string> tokens = new();
        int? inboxUserId = null;
        var inboxRole = role;
        switch (model.TargetMode)
        {
            case "user":
                if (model.UserId is null or <= 0)
                {
                    ModelState.AddModelError(nameof(model.UserId), "Enter a user id.");
                    break;
                }
                inboxUserId = model.UserId;
                tokens = await _db.UserDeviceTokens.AsNoTracking()
                    .Where(t => t.UserId == model.UserId && (model.IgnoreRole || t.UserType == role))
                    .Select(t => t.DeviceToken).ToListAsync(cancellationToken);
                break;
            case "device":
                var device = model.TokenId == null
                    ? null
                    : await _db.UserDeviceTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == model.TokenId, cancellationToken);
                if (device == null)
                {
                    ModelState.AddModelError(nameof(model.TokenId), "Choose a registered device.");
                    break;
                }
                inboxUserId = device.UserId;
                inboxRole = model.IgnoreRole ? role : device.UserType;
                tokens = new List<string> { device.DeviceToken };
                break;
            case "role":
                tokens = await _db.UserDeviceTokens.AsNoTracking()
                    .Where(t => t.UserType == role && (model.Platform == "all" || t.Platform == model.Platform))
                    .Select(t => t.DeviceToken).ToListAsync(cancellationToken);
                break;
            default:
                ModelState.AddModelError(nameof(model.TargetMode), "Choose who to send to.");
                break;
        }

        if (model.SaveToInbox && inboxUserId != null
            && !await _notifications.UserExistsAsync(inboxUserId.Value, inboxRole, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.SaveToInbox),
                $"User {inboxUserId} has no active {inboxRole} profile, so an inbox row can't be saved for that role.");
        }

        if (!ModelState.IsValid) return View("Index", model);

        string title, body, type, screen;
        if (isCustom)
        {
            title = model.CustomTitle!.Trim();
            body = model.CustomBody!.Trim();
            type = string.IsNullOrWhiteSpace(model.CustomType) ? "custom" : model.CustomType.Trim();
            screen = model.CustomScreen?.Trim() ?? string.Empty;
        }
        else
        {
            (title, body) = entry!.Build(model.ServiceName ?? string.Empty, model.ProviderName ?? string.Empty, model.Reason ?? string.Empty);
            type = entry.Type;
            screen = entry.Screen;
        }

        var data = new Dictionary<string, string>
        {
            ["type"] = type,
            ["screen"] = screen,
            ["booking_id"] = model.BookingId?.ToString() ?? string.Empty,
            ["request_id"] = model.RequestId?.ToString() ?? string.Empty
        };
        foreach (var (key, value) in ParseExtraData(model.ExtraData))
            data[key] = value;

        var inboxSaved = false;
        if (model.SaveToInbox && inboxUserId != null)
        {
            var row = new UserNotification
            {
                UserId = inboxUserId.Value,
                UserType = inboxRole,
                Type = type.Length <= 40 ? type : type[..40],
                Title = title.Length <= 200 ? title : title[..200],
                Body = body.Length <= 500 ? body : body[..500],
                Screen = screen.Length <= 40 ? screen : screen[..40],
                BookingUid = model.BookingId,
                RequestUid = model.RequestId,
                CreatedAt = DateTime.UtcNow
            };
            _db.UserNotifications.Add(row);
            await _db.SaveChangesAsync(cancellationToken);
            data["notification_id"] = row.Id.ToString();
            inboxSaved = true;
        }

        // Unticked = follow Notifications:AndroidChannelsEnabled; ticked = force the channel for this send, to try
        // a new app build's channels before the setting is switched on for everyone.
        BroadcastResult result;
        if (type == NotificationTypes.AppUpdate)
        {
            // Same rule as the broadcast: each platform's tokens get that platform's own latest_version / store_url.
            var platformOf = await _db.UserDeviceTokens.AsNoTracking()
                .Where(t => tokens.Contains(t.DeviceToken))
                .Select(t => new { t.DeviceToken, t.Platform })
                .ToListAsync(cancellationToken);
            var groups = platformOf.GroupBy(t => t.Platform).ToList();

            var effective = await _policies.GetEffectiveAsync(cancellationToken);
            var payloads = new Dictionary<string, Dictionary<string, string>>();
            foreach (var g in groups)
            {
                if (AppUpdatePayload.TryBuild(effective, g.Key, out var payload, out var error,
                        string.IsNullOrWhiteSpace(model.LatestVersionOverride) ? null : model.LatestVersionOverride.Trim(), model.ForceUpdate))
                    payloads[g.Key] = payload;
                else
                    ModelState.AddModelError(string.Empty, error!);
            }
            if (!ModelState.IsValid) return View("Index", model);

            result = new BroadcastResult(true, 0, 0, 0, 0);
            foreach (var g in groups)
            {
                var groupData = new Dictionary<string, string>(data);
                foreach (var (key, value) in payloads[g.Key]) groupData[key] = value;
                var part = await _notifications.SendToDevicesAsync(g.Select(t => t.DeviceToken).ToList(), title, body,
                    groupData, model.UseAndroidChannel ? true : null, cancellationToken);
                result = new BroadcastResult(result.FirebaseConfigured && part.FirebaseConfigured,
                    result.Recipients + part.Recipients, result.Sent + part.Sent,
                    result.Failed + part.Failed, result.RemovedStale + part.RemovedStale);
            }
        }
        else
        {
            result = await _notifications.SendToDevicesAsync(
                tokens, title, body, data, model.UseAndroidChannel ? true : null, cancellationToken);
        }

        _logger.LogInformation(
            "Push tester by {User}: mode={Mode}, type={Type}, role={Role}, recipients={Recipients}, sent={Sent}, failed={Failed}, inbox={Inbox}.",
            User.Identity?.Name, model.TargetMode, type, role, result.Recipients, result.Sent, result.Failed, inboxSaved);

        if (!result.FirebaseConfigured)
            TempData["ErrorMessage"] = "Push is not configured on this server (Firebase service account missing), so nothing was sent.";
        else if (tokens.Count == 0)
            TempData["ErrorMessage"] = "No matching registered devices, so nothing was pushed" +
                (inboxSaved ? " (the inbox row was still saved)." : ".");

        model.Result = new PushTesterResult
        {
            Title = title, Body = body, Type = type, Screen = screen,
            ChannelId = NotificationChannels.For(type),
            ChannelSent = model.UseAndroidChannel || _channelsSetting.CurrentValue.AndroidChannelsEnabled,
            Recipients = result.Recipients, Sent = result.Sent, Failed = result.Failed,
            RemovedStale = result.RemovedStale, InboxSaved = inboxSaved
        };
        return View("Index", model);
    }

    private async Task FillAsync(PushTesterFormVm vm, CancellationToken cancellationToken)
    {
        vm.FirebaseConfigured = FirebaseApp.DefaultInstance != null;

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

        vm.Devices = tokens;
    }

    /// <summary>"key=value" per line; at most 10 pairs, keys limited to simple names so they can't clash with FCM internals.</summary>
    private static IEnumerable<KeyValuePair<string, string>> ParseExtraData(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var count = 0;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var i = line.IndexOf('=');
            if (i <= 0) continue;
            var key = line[..i].Trim();
            if (key.Length > 40 || !key.All(c => char.IsLetterOrDigit(c) || c == '_')) continue;
            if (key.StartsWith("google", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("gcm", StringComparison.OrdinalIgnoreCase)
                || key.Equals("from", StringComparison.OrdinalIgnoreCase)) continue;

            yield return new KeyValuePair<string, string>(key, line[(i + 1)..].Trim());
            if (++count >= 10) yield break;
        }
    }
}

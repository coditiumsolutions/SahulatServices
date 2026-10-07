using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public record ReleaseTarget(string Scope, string? Platform, int? UserId, int? DeviceTokenId);

/// <summary>Outcome of a release attempt. Error is set (and nothing was sent or written) when it was rejected.</summary>
public record ReleaseOutcome(string? Error, BroadcastResult? Result);

/// <summary>
/// Admin "release blocked devices": resolves the chosen scope to device tokens, sends the silent app_unblock push and
/// records the send in UpdateBlockReleases (append-only). There is deliberately no mobile endpoint for this.
/// </summary>
public interface IUpdateBlockReleaseService
{
    static readonly string[] Scopes = { "everyone", "android", "ios", "user", "device" };

    /// <summary>Registered devices the scope reaches, or an error for an unknown user/device or a bad scope.</summary>
    Task<(IReadOnlyList<DeviceTarget> Devices, string? Error)> ResolveAsync(ReleaseTarget target, CancellationToken cancellationToken = default);

    Task<ReleaseOutcome> ReleaseAsync(ReleaseTarget target, string reason, string adminName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Latest ReleasedAtUtc (UTC) among the releases that apply to a caller, or null: everyone, the caller's platform,
    /// and (only when <paramref name="deviceToken"/> matches a registered token) that token's user or device. Read-only.
    /// </summary>
    Task<DateTime?> GetLastReleaseAtAsync(string platform, string? deviceToken, CancellationToken cancellationToken = default);
}

public class UpdateBlockReleaseService : IUpdateBlockReleaseService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<UpdateBlockReleaseService> _logger;

    public UpdateBlockReleaseService(AppDbContext db, INotificationService notifications,
        ILogger<UpdateBlockReleaseService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<DeviceTarget> Devices, string? Error)> ResolveAsync(
        ReleaseTarget target, CancellationToken cancellationToken = default)
    {
        var query = _db.UserDeviceTokens.AsNoTracking().AsQueryable();
        switch (target.Scope)
        {
            case "everyone":
                break;
            case "android":
            case "ios":
                query = query.Where(t => t.Platform == target.Scope);
                break;
            case "user":
                if (target.UserId is null or <= 0)
                    return (Array.Empty<DeviceTarget>(), "Enter a user id.");
                if (!await _db.UsersLogins.AsNoTracking().AnyAsync(u => u.Uid == target.UserId, cancellationToken))
                    return (Array.Empty<DeviceTarget>(), $"User {target.UserId} does not exist.");
                query = query.Where(t => t.UserId == target.UserId);
                break;
            case "device":
                if (target.DeviceTokenId is null or <= 0)
                    return (Array.Empty<DeviceTarget>(), "Choose a registered device.");
                if (!await _db.UserDeviceTokens.AsNoTracking().AnyAsync(t => t.Id == target.DeviceTokenId, cancellationToken))
                    return (Array.Empty<DeviceTarget>(), $"Device {target.DeviceTokenId} is not registered.");
                query = query.Where(t => t.Id == target.DeviceTokenId);
                break;
            default:
                return (Array.Empty<DeviceTarget>(), "Choose who to release.");
        }

        // Device-wide: no role filter, so a user's Client and Provider tokens are both included.
        var devices = await query.Select(t => new DeviceTarget(t.DeviceToken, t.Platform))
            .ToListAsync(cancellationToken);
        return (devices, null);
    }

    public async Task<DateTime?> GetLastReleaseAtAsync(string platform, string? deviceToken,
        CancellationToken cancellationToken = default)
    {
        var token = string.IsNullOrWhiteSpace(deviceToken) ? null : deviceToken.Trim();

        // One MAX query. The user/device branches only match when the token is registered, so an unknown token behaves
        // exactly like no token (same answer, nothing revealed).
        var latest = await _db.UpdateBlockReleases.AsNoTracking()
            .Where(r =>
                r.Scope == "everyone"
                || ((r.Scope == "android" || r.Scope == "ios") && r.Platform == platform)
                || (token != null && r.Scope == "user"
                    && _db.UserDeviceTokens.Any(t => t.DeviceToken == token && t.UserId == r.UserId))
                || (token != null && r.Scope == "device"
                    && _db.UserDeviceTokens.Any(t => t.Id == r.DeviceTokenId && t.DeviceToken == token)))
            .MaxAsync(r => (DateTime?)r.ReleasedAtUtc, cancellationToken);

        return latest.HasValue ? DateTime.SpecifyKind(latest.Value, DateTimeKind.Utc) : null;
    }

    public async Task<ReleaseOutcome> ReleaseAsync(ReleaseTarget target, string reason, string adminName,
        CancellationToken cancellationToken = default)
    {
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length == 0) return new ReleaseOutcome("Enter a reason.", null);
        if (reason.Length > 200) return new ReleaseOutcome("The reason can be at most 200 characters.", null);

        var (devices, error) = await ResolveAsync(target, cancellationToken);
        if (error != null) return new ReleaseOutcome(error, null);
        if (devices.Count == 0) return new ReleaseOutcome("No registered devices match, so nothing was sent.", null);

        // One instant: it is sent as sent_at and stored as ReleasedAtUtc.
        var releasedAt = DateTime.UtcNow;
        var result = await _notifications.SendUpdateUnblockAsync(devices, releasedAt, cancellationToken);
        if (!result.FirebaseConfigured)
            return new ReleaseOutcome("Push is not configured on this server (Firebase service account missing), so nothing was sent.", null);

        _db.UpdateBlockReleases.Add(new UpdateBlockRelease
        {
            Scope = target.Scope,
            Platform = target.Scope is "android" or "ios" ? target.Scope : null,
            UserId = target.Scope == "user" ? target.UserId : null,
            DeviceTokenId = target.Scope == "device" ? target.DeviceTokenId : null,
            ReleasedAtUtc = releasedAt,
            ReleasedBy = adminName.Length <= 100 ? adminName : adminName[..100],
            Reason = reason,
            Recipients = result.Recipients,
            Sent = result.Sent,
            Failed = result.Failed
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Update block release by {Admin}: scope={Scope}, user={UserId}, device={DeviceId}, reason={Reason}, recipients={Recipients}, sent={Sent}, failed={Failed}, removedStale={Removed}.",
            adminName, target.Scope, target.UserId, target.DeviceTokenId, reason, result.Recipients, result.Sent, result.Failed, result.RemovedStale);
        return new ReleaseOutcome(null, result);
    }
}

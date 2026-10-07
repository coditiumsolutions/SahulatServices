using System.Globalization;
using System.Text.Json;
using HomeServicesPortal.Controllers.Api;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Options;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

/// <summary>
/// GET /api/v1/app/config last_unblock_at (api.txt v3.40). Runs against an in-memory SQLite database, so nothing
/// touches the real DB. Only the two tables the lookup reads are created.
/// </summary>
public class AppConfigLastUnblockTests : IDisposable
{
    private const string TokenA = "token-a-device";
    private const string TokenB = "token-b-other-user";
    private const string Unregistered = "token-nobody-has";

    private static readonly DateTime T1 = new(2026, 10, 6, 7, 0, 18, DateTimeKind.Utc);
    private static readonly DateTime T2 = T1.AddHours(2);
    private static readonly DateTime T3 = T1.AddHours(5);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public AppConfigLastUnblockTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE UserDeviceTokens (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, UserType TEXT NOT NULL,
                DeviceToken TEXT NOT NULL, Platform TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
            CREATE TABLE UpdateBlockReleases (Id INTEGER PRIMARY KEY AUTOINCREMENT, Scope TEXT NOT NULL, Platform TEXT NULL,
                UserId INTEGER NULL, DeviceTokenId INTEGER NULL, ReleasedAtUtc TEXT NOT NULL, ReleasedBy TEXT NOT NULL,
                Reason TEXT NOT NULL, Recipients INTEGER NOT NULL, Sent INTEGER NOT NULL, Failed INTEGER NOT NULL);";
        cmd.ExecuteNonQuery();

        // device 1 = user 10 on android (token A), device 2 = user 20 on android (token B).
        using var db = new AppDbContext(_options);
        db.UserDeviceTokens.AddRange(
            new UserDeviceToken { UserId = 10, UserType = "Client", DeviceToken = TokenA, Platform = "android", UpdatedAt = T1 },
            new UserDeviceToken { UserId = 20, UserType = "Client", DeviceToken = TokenB, Platform = "android", UpdatedAt = T1 });
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private void AddRelease(string scope, DateTime at, string? platform = null, int? userId = null, int? deviceId = null)
    {
        using var db = new AppDbContext(_options);
        db.UpdateBlockReleases.Add(new UpdateBlockRelease
        {
            Scope = scope, Platform = platform, UserId = userId, DeviceTokenId = deviceId, ReleasedAtUtc = at,
            ReleasedBy = "test", Reason = "test", Recipients = 1, Sent = 1
        });
        db.SaveChanges();
    }

    private async Task<(IActionResult Result, DefaultHttpContext Http)> CallAsync(string? platform, string? token)
    {
        var db = new AppDbContext(_options);
        var service = new UpdateBlockReleaseService(db, null!, NullLogger<UpdateBlockReleaseService>.Instance);
        var http = new DefaultHttpContext();
        var controller = new AppConfigApiController(new FakePolicies(), service)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        return (await controller.GetConfig(platform, token, CancellationToken.None), http);
    }

    private async Task<string?> LastUnblockAsync(string platform, string? token)
    {
        var (result, _) = await CallAsync(platform, token);
        var dto = Assert.IsType<AppConfigApiDto>(Assert.IsType<OkObjectResult>(result).Value);
        return dto.LastUnblockAt;
    }

    private static string Iso(DateTime utc) => utc.ToString("O");

    [Fact]
    public async Task NoReleases_ReturnsNull_ButFieldIsAlwaysPresentInJson()
    {
        var (result, _) = await CallAsync("android", null);
        var dto = Assert.IsType<AppConfigApiDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Null(dto.LastUnblockAt);

        var json = JsonSerializer.Serialize(dto);
        Assert.Contains("\"last_unblock_at\":null", json);
        // Existing fields are untouched.
        Assert.Contains("\"minimum_required_version\":\"1.0.3\"", json);
        Assert.Contains("\"latest_version\":\"1.0.5\"", json);
        Assert.Contains("\"force_update\":false", json);
        Assert.Contains("\"store_url\":\"https://store/android\"", json);
        Assert.Contains("\"update_message\":\"msg-android\"", json);
    }

    [Fact]
    public async Task ResponseShape_OnlyAddsLastUnblockAt()
    {
        var (result, _) = await CallAsync("ios", null);
        var dto = Assert.IsType<AppConfigApiDto>(Assert.IsType<OkObjectResult>(result).Value);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto));
        var names = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(new[]
        {
            "minimum_required_version", "latest_version", "force_update", "store_url", "update_message", "last_unblock_at"
        }, names);
    }

    [Fact]
    public async Task Everyone_AppliesToAnyCaller_AndTheLatestWins()
    {
        AddRelease("everyone", T1);
        Assert.Equal(Iso(T1), await LastUnblockAsync("android", null));
        Assert.Equal(Iso(T1), await LastUnblockAsync("ios", null));

        AddRelease("everyone", T3);
        AddRelease("everyone", T2); // inserted later but older: MAX, not "last row"
        Assert.Equal(Iso(T3), await LastUnblockAsync("ios", null));
    }

    [Fact]
    public async Task Value_IsUtcIso8601_WithZ()
    {
        AddRelease("everyone", T1);
        var value = await LastUnblockAsync("android", null);
        Assert.EndsWith("Z", value);
        Assert.Equal(T1, DateTime.Parse(value!, null, DateTimeStyles.RoundtripKind));
    }

    [Fact]
    public async Task PlatformScope_AppliesOnlyToThatPlatform_CaseInsensitiveParam()
    {
        AddRelease("ios", T2, platform: "ios");
        Assert.Equal(Iso(T2), await LastUnblockAsync("ios", null));
        Assert.Equal(Iso(T2), await LastUnblockAsync(" IOS ", null));
        Assert.Null(await LastUnblockAsync("android", null));

        AddRelease("android", T3, platform: "android");
        Assert.Equal(Iso(T3), await LastUnblockAsync("android", null));
        Assert.Equal(Iso(T2), await LastUnblockAsync("ios", null));
    }

    [Fact]
    public async Task UserScope_AppliesOnlyWhenTokenBelongsToThatUser()
    {
        AddRelease("user", T2, userId: 10);
        Assert.Equal(Iso(T2), await LastUnblockAsync("android", TokenA));   // matching token
        Assert.Null(await LastUnblockAsync("android", TokenB));             // another user's token
        Assert.Null(await LastUnblockAsync("android", Unregistered));       // unregistered token
        Assert.Null(await LastUnblockAsync("android", null));               // no token
        Assert.Null(await LastUnblockAsync("android", "   "));              // blank token
    }

    [Fact]
    public async Task DeviceScope_AppliesOnlyToThatDevicesToken()
    {
        AddRelease("device", T2, deviceId: 1); // device 1 = TokenA
        Assert.Equal(Iso(T2), await LastUnblockAsync("android", TokenA));
        Assert.Null(await LastUnblockAsync("android", TokenB));
        Assert.Null(await LastUnblockAsync("android", Unregistered));
        Assert.Null(await LastUnblockAsync("android", null));
    }

    [Fact]
    public async Task UnknownToken_GetsSameAnswerAsNoToken_AndNothingIsRevealed()
    {
        AddRelease("everyone", T1);
        AddRelease("user", T3, userId: 10);
        AddRelease("device", T3, deviceId: 1);

        var none = await LastUnblockAsync("android", null);
        var unknown = await LastUnblockAsync("android", Unregistered);
        Assert.Equal(none, unknown);
        Assert.Equal(Iso(T1), unknown);
        // A registered token sees its own, later, releases.
        Assert.Equal(Iso(T3), await LastUnblockAsync("android", TokenA));
        Assert.Equal(Iso(T1), await LastUnblockAsync("android", TokenB));
    }

    [Fact]
    public async Task ReleaseBeforeAndAfter_ComparesAsTheAppNeeds()
    {
        AddRelease("everyone", T2);
        var value = DateTime.Parse((await LastUnblockAsync("android", null))!, null, DateTimeStyles.RoundtripKind);
        Assert.True(value > T1);   // block created before the release: cleared
        Assert.False(value > T3);  // block created after the release: not cleared
    }

    [Fact]
    public async Task OversizedToken_IsTreatedAsNoToken_NotAnError()
    {
        AddRelease("user", T2, userId: 10);
        Assert.Null(await LastUnblockAsync("android", new string('x', 600)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("windows")]
    public async Task BadOrMissingPlatform_Still400(string? platform)
    {
        AddRelease("everyone", T1);
        var (result, _) = await CallAsync(platform, TokenA);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(body.Success);
        Assert.Equal("platform must be 'android' or 'ios'.", body.Message);
    }

    [Fact]
    public async Task Response_IsNoStore_OnSuccessAndOnFailure()
    {
        var (_, ok) = await CallAsync("android", TokenA);
        Assert.Equal("no-store", ok.Response.Headers.CacheControl.ToString());
        var (_, bad) = await CallAsync("nope", null);
        Assert.Equal("no-store", bad.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Lookup_IsReadOnly()
    {
        AddRelease("user", T2, userId: 10);
        await LastUnblockAsync("android", TokenA);
        await LastUnblockAsync("ios", Unregistered);
        await using var db = new AppDbContext(_options);
        Assert.Equal(2, await db.UserDeviceTokens.CountAsync());
        Assert.Equal(1, await db.UpdateBlockReleases.CountAsync());
        Assert.Equal(T1, (await db.UserDeviceTokens.FirstAsync()).UpdatedAt);
    }

    private sealed class FakePolicies : IAppVersionPolicyService
    {
        public Task<AppConfigOptions> GetEffectiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppConfigOptions
            {
                Android = new AppVersionPolicy
                {
                    MinimumRequiredVersion = "1.0.3", LatestVersion = "1.0.5", ForceUpdate = false,
                    StoreUrl = "https://store/android", UpdateMessage = "msg-android"
                },
                Ios = new AppVersionPolicy
                {
                    MinimumRequiredVersion = "1.0.1", LatestVersion = "1.0.2", ForceUpdate = false,
                    StoreUrl = "https://store/ios", UpdateMessage = "msg-ios"
                }
            });

        public Task SaveLatestVersionAsync(string platform, string version, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

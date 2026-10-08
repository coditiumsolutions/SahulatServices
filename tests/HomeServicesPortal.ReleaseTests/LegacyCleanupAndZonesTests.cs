using System.ComponentModel.DataAnnotations;
using HomeServicesPortal.Controllers.Api;
using HomeServicesPortal.Data;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

/// <summary>Request validation (legacy removal) and the ProviderZones / EstimateText changes, on in-memory SQLite.</summary>
public class LegacyCleanupAndZonesTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public LegacyCleanupAndZonesTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Providers (UID INTEGER PRIMARY KEY AUTOINCREMENT, Zone TEXT NULL);
            CREATE TABLE ProviderZones (UID INTEGER PRIMARY KEY AUTOINCREMENT, ProviderUID INTEGER NOT NULL,
                ZoneName TEXT NOT NULL, CreatedOn TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE (ProviderUID, ZoneName));
            CREATE TABLE ServiceCategories (UID INTEGER PRIMARY KEY, ServiceUID INTEGER NOT NULL DEFAULT 1,
                CategoryName TEXT NOT NULL, IsActive INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE ServiceTitles (UID INTEGER PRIMARY KEY, CategoryUID INTEGER NOT NULL, Title TEXT NOT NULL,
                Description TEXT NULL, BasePrice TEXT NULL, EstimateText TEXT NULL, DisplayOrder INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1, CreatedOn TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            INSERT INTO Providers (UID, Zone) VALUES (1, NULL), (2, 'Zone 4');
            INSERT INTO ProviderZones (ProviderUID, ZoneName) VALUES (2, 'Zone 4');
            INSERT INTO ServiceCategories (UID, CategoryName) VALUES (1, 'Pest Control');
            INSERT INTO ServiceTitles (UID, CategoryUID, Title, BasePrice, EstimateText) VALUES
                (10, 1, 'Termite', 20000, '18000-22000'),
                (11, 1, 'Fumigation', 1500, NULL),
                (12, 1, 'Rodents', NULL, 'From 1500');";
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FakeConfigs : IConfigurationEntryService
    {
        public Task<IReadOnlyList<string>> GetValuesByKeyAsync(string configKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(new[] { "Bahria 1-8", "Zone 2", "Zone 3", "Zone 4", "Zone 5" });

        public Task<ConfigurationListVm> GetListAsync(string? search, int page, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConfigurationDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConfigurationFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConfigurationDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> CreateAsync(ConfigurationFormVm model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdateAsync(ConfigurationFormVm model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private ProviderZoneService Zones() => new(new AppDbContext(_options), new FakeConfigs());

    private string? ProviderZoneColumn(int uid)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT Zone FROM Providers WHERE UID = {uid}";
        var v = cmd.ExecuteScalar();
        return v is DBNull or null ? null : (string)v;
    }

    // ---------- register-provider: single-category path removed ----------

    private static List<string> Errors(RegisterProviderRequest r) =>
        r.Validate(new ValidationContext(r)).Select(v => v.ErrorMessage ?? "").ToList();

    [Fact]
    public void Register_WithCategoryIdsAndPrimary_IsValid() =>
        Assert.Empty(Errors(new RegisterProviderRequest { CategoryIds = new() { 1, 3 }, PrimaryCategoryId = 1 }));

    [Fact]
    public void Register_WithoutCategoryIds_IsRejected()
    {
        var errors = Errors(new RegisterProviderRequest());
        Assert.Contains(errors, e => e.Contains("CategoryIds is required"));
    }

    [Fact]
    public void Register_EmptyCategoryIds_IsRejected() =>
        Assert.NotEmpty(Errors(new RegisterProviderRequest { CategoryIds = new(), PrimaryCategoryId = 1 }));

    [Fact]
    public void Register_PrimaryMissing_IsRejected() =>
        Assert.Contains(Errors(new RegisterProviderRequest { CategoryIds = new() { 1 } }), e => e.Contains("PrimaryCategoryId is required"));

    [Fact]
    public void Register_PrimaryNotInList_IsRejected() =>
        Assert.Contains(Errors(new RegisterProviderRequest { CategoryIds = new() { 1, 2 }, PrimaryCategoryId = 9 }), e => e.Contains("one of"));

    [Fact]
    public void Register_ZonesAreOptional() =>
        Assert.Empty(Errors(new RegisterProviderRequest { CategoryIds = new() { 1 }, PrimaryCategoryId = 1, Zones = null }));

    [Fact]
    public void RegisterRequest_NoLongerHasSingleCategoryFields()
    {
        Assert.Null(typeof(RegisterProviderRequest).GetProperty("CategoryId"));
        Assert.Null(typeof(RegisterProviderRequest).GetProperty("CategoryName"));
    }

    // ---------- client request status: Pending no longer accepted ----------

    [Fact]
    public void ClientEditableStatuses_AreInitiatedAndCancelledOnly()
    {
        var statuses = RequestStatusConstants.ClientEditableStatuses;
        Assert.Contains("Initiated", statuses);
        Assert.Contains("Cancelled", statuses);
        Assert.DoesNotContain(statuses, s => s.Equals("Pending", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExistingPendingRows_StillCountAsUnassigned() =>
        Assert.True(RequestStatusConstants.IsUnassigned("Pending"));

    // ---------- zones ----------

    [Fact]
    public async Task Resolve_CanonicalizesCaseAndDedupes()
    {
        var (zones, error) = await Zones().ResolveZonesAsync(new[] { "zone 3", " ZONE 3 ", "bahria 1-8" }, null);
        Assert.Null(error);
        Assert.Equal(new[] { "Zone 3", "Bahria 1-8" }, zones);
    }

    [Fact]
    public async Task Resolve_UnknownZone_IsAnError()
    {
        var (zones, error) = await Zones().ResolveZonesAsync(new[] { "Zone 3", "Mars" }, null);
        Assert.Null(zones);
        Assert.Contains("'Mars'", error);
    }

    [Fact]
    public async Task Resolve_NullOrBlank_IsEmptyNotError()
    {
        var (a, ea) = await Zones().ResolveZonesAsync(null, null);
        var (b, eb) = await Zones().ResolveZonesAsync(new[] { "", "  " }, null);
        Assert.Null(ea); Assert.Null(eb);
        Assert.Empty(a!); Assert.Empty(b!);
    }

    [Fact]
    public async Task Update_ReplacesSet_AndRefreshesDisplayColumn()
    {
        var svc = Zones();
        var (ok, err) = await svc.UpdateZonesAsync(1, new[] { "Zone 5", "Bahria 1-8" });
        Assert.True(ok, err);
        Assert.Equal(new[] { "Bahria 1-8", "Zone 5" }, await Zones().GetZonesAsync(1));
        Assert.Equal("Bahria 1-8, Zone 5", ProviderZoneColumn(1));

        (ok, _) = await Zones().UpdateZonesAsync(1, new[] { "Zone 2" });
        Assert.True(ok);
        Assert.Equal(new[] { "Zone 2" }, await Zones().GetZonesAsync(1));
        Assert.Equal("Zone 2", ProviderZoneColumn(1));
    }

    [Fact]
    public async Task Update_IsIdempotent()
    {
        for (var i = 0; i < 3; i++)
        {
            var (ok, err) = await Zones().UpdateZonesAsync(1, new[] { "Zone 3", "Zone 2" });
            Assert.True(ok, err);
        }
        Assert.Equal(new[] { "Zone 2", "Zone 3" }, await Zones().GetZonesAsync(1));
    }

    [Fact]
    public async Task Update_EmptyList_ClearsZonesAndColumn()
    {
        var (ok, _) = await Zones().UpdateZonesAsync(2, Array.Empty<string>());
        Assert.True(ok);
        Assert.Empty(await Zones().GetZonesAsync(2));
        Assert.Null(ProviderZoneColumn(2));
    }

    [Fact]
    public async Task Update_UnknownZone_ChangesNothing()
    {
        var (ok, err) = await Zones().UpdateZonesAsync(2, new[] { "Zone 3", "Mars" });
        Assert.False(ok);
        Assert.Contains("Mars", err);
        Assert.Equal(new[] { "Zone 4" }, await Zones().GetZonesAsync(2));
        Assert.Equal("Zone 4", ProviderZoneColumn(2));
    }

    [Fact]
    public async Task Update_MoreThanTwoZones_IsRejected_AndChangesNothing()
    {
        var (ok, err) = await Zones().UpdateZonesAsync(2, new[] { "Zone 2", "Zone 3", "Zone 5" });
        Assert.False(ok);
        Assert.Equal("A provider can have at most 2 zones.", err);
        Assert.Equal(new[] { "Zone 4" }, await Zones().GetZonesAsync(2));

        (ok, err) = await Zones().UpdateZonesAsync(2, new[] { "Zone 2", "zone 2", "Zone 3" });
        Assert.True(ok, err);
        Assert.Equal(new[] { "Zone 2", "Zone 3" }, await Zones().GetZonesAsync(2));
    }

    [Fact]
    public async Task Update_UnknownProvider_IsAnError()
    {
        var (ok, err) = await Zones().UpdateZonesAsync(999, new[] { "Zone 3" });
        Assert.False(ok);
        Assert.Equal("Provider not found.", err);
    }

    [Fact]
    public async Task Update_KeepsAZoneRemovedFromConfig()
    {
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO ProviderZones (ProviderUID, ZoneName) VALUES (1, 'Old Zone')";
            cmd.ExecuteNonQuery();
        }
        var (ok, err) = await Zones().UpdateZonesAsync(1, new[] { "Old Zone", "Zone 3" });
        Assert.True(ok, err);
        Assert.Equal(new[] { "Old Zone", "Zone 3" }, await Zones().GetZonesAsync(1));
    }

    [Fact]
    public async Task Controller_Put_BadZone_Is400_AndGood_Is200()
    {
        var controller = new ZonesApiController(Zones());
        var bad = await controller.UpdateProviderZones(1, new UpdateProviderZonesRequestDto { Zones = new() { "Mars" } }, default);
        Assert.IsType<BadRequestObjectResult>(bad.Result);

        var good = await controller.UpdateProviderZones(1, new UpdateProviderZonesRequestDto { Zones = new() { "Zone 3" } }, default);
        var body = Assert.IsType<ApiResponse<List<string>>>(Assert.IsType<OkObjectResult>(good.Result).Value);
        Assert.Equal(new[] { "Zone 3" }, body.Data);

        var options = await controller.GetOptions(default);
        var opts = Assert.IsType<ApiResponse<List<string>>>(Assert.IsType<OkObjectResult>(options.Result).Value);
        Assert.Equal(5, opts.Data!.Count);
    }

    // ---------- service title estimateText ----------

    [Fact]
    public async Task ServiceTitlesApi_ReturnsEstimateText_AndKeepsBasePrice()
    {
        var svc = new ServiceTitleService(new AppDbContext(_options));
        var list = await svc.GetActiveTitlesForApiAsync(1);
        var termite = list.Single(t => t.Id == 10);
        Assert.Equal("18000-22000", termite.EstimateText);
        Assert.Equal(20000m, termite.BasePrice);

        Assert.Null(list.Single(t => t.Id == 11).EstimateText);
        Assert.Equal(1500m, list.Single(t => t.Id == 11).BasePrice);
        var textOnly = list.Single(t => t.Id == 12);
        Assert.Equal("From 1500", textOnly.EstimateText);
        Assert.Null(textOnly.BasePrice);

        var single = await svc.GetActiveTitleForApiAsync(10);
        Assert.Equal("18000-22000", single!.EstimateText);
    }
}

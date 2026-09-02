using HomeServicesPortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HomeServicesPortal.Tests;

/// <summary>
/// Shared, real-database fixture for the status-workflow integration tests. Reads the same
/// appsettings.json / appsettings.&lt;Environment&gt;.json the app itself uses (walks up from the
/// test binary's output folder to find the HomeServicesPortal project directory), so it hits
/// whatever DB the app is currently configured against — no separate test connection string to
/// keep in sync.
///
/// Inserts one throwaway Client + Provider + ClientAddress (clearly marked, see MobileNo/FullName
/// prefixes below) that individual tests build their CustomerServiceRequest/ServiceBooking rows
/// on top of. Everything this fixture inserts is deleted in DisposeAsync, whether the tests
/// passed or failed.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private const string TestMarker = "AUTOTEST-StatusWorkflow";

    public int ClientUid { get; private set; }
    public int ProviderUid { get; private set; }
    public int ClientAddressUid { get; private set; }
    public int CategoryUid { get; private set; }

    private int _clientUserUid;
    private int _providerUserUid;
    private DbContextOptions<AppDbContext> _options = null!;

    public AppDbContext CreateContext() => new(_options);

    public async Task InitializeAsync()
    {
        var connectionString = LoadConnectionString();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = CreateContext();

        var category = await db.ServiceCategories.AsNoTracking().FirstOrDefaultAsync(c => c.IsActive)
            ?? throw new InvalidOperationException(
                "No active ServiceCategory found to run tests against — seed at least one active category.");
        CategoryUid = category.Uid;

        var suffix = (DateTime.UtcNow.Ticks % 1_000_000_000_000).ToString();

        var clientUser = new Entities.UsersLogin
        {
            MobileNo = $"9{suffix}"[..Math.Min(20, $"9{suffix}".Length)],
            PasswordHash = "AUTOTEST-not-a-real-hash",
            UserType = "Client",
            IsActive = true,
            IsVerified = true,
            CreatedOn = DateTime.Now
        };
        var providerUser = new Entities.UsersLogin
        {
            MobileNo = $"8{suffix}"[..Math.Min(20, $"8{suffix}".Length)],
            PasswordHash = "AUTOTEST-not-a-real-hash",
            UserType = "Provider",
            IsActive = true,
            IsVerified = true,
            CreatedOn = DateTime.Now
        };
        db.UsersLogins.AddRange(clientUser, providerUser);
        await db.SaveChangesAsync();

        _clientUserUid = clientUser.Uid;
        _providerUserUid = providerUser.Uid;

        var client = new Entities.Client
        {
            UserUid = clientUser.Uid,
            FullName = $"{TestMarker} Client",
            CreatedOn = DateTime.Now
        };
        var provider = new Entities.Provider
        {
            UserUid = providerUser.Uid,
            FullName = $"{TestMarker} Provider",
            Cnic = "0-0-0",
            CategoryUid = CategoryUid,
            CreatedOn = DateTime.Now
        };
        db.Clients.Add(client);
        db.Providers.Add(provider);
        await db.SaveChangesAsync();

        ClientUid = client.Uid;
        ProviderUid = provider.Uid;

        var address = new Entities.ClientAddress
        {
            ClientUid = ClientUid,
            AddressTitle = $"{TestMarker} Address",
            FullAddress = "123 Test Street",
            Area = "Test Area",
            City = "Test City"
        };
        db.ClientAddresses.Add(address);
        await db.SaveChangesAsync();

        ClientAddressUid = address.Uid;
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();

        // Delete in FK-safe order. Any CustomerServiceRequests/ServiceBookings left behind by a
        // test that threw before its own cleanup ran are also swept up here, scoped strictly to
        // this fixture's own ClientUid so a failed test can never touch unrelated data.
        var bookings = await db.ServiceBookings.Where(b => b.ClientUid == ClientUid).ToListAsync();
        db.ServiceBookings.RemoveRange(bookings);

        var requests = await db.CustomerServiceRequests.Where(r => r.ClientUid == ClientUid).ToListAsync();
        db.CustomerServiceRequests.RemoveRange(requests);

        await db.SaveChangesAsync();

        var address = await db.ClientAddresses.FirstOrDefaultAsync(a => a.Uid == ClientAddressUid);
        if (address != null) db.ClientAddresses.Remove(address);

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Uid == ClientUid);
        if (client != null) db.Clients.Remove(client);

        var provider = await db.Providers.FirstOrDefaultAsync(p => p.Uid == ProviderUid);
        if (provider != null) db.Providers.Remove(provider);

        await db.SaveChangesAsync();

        var clientUser = await db.UsersLogins.FirstOrDefaultAsync(u => u.Uid == _clientUserUid);
        if (clientUser != null) db.UsersLogins.Remove(clientUser);

        var providerUser = await db.UsersLogins.FirstOrDefaultAsync(u => u.Uid == _providerUserUid);
        if (providerUser != null) db.UsersLogins.Remove(providerUser);

        await db.SaveChangesAsync();
    }

    private static string LoadConnectionString()
    {
        var projectDir = FindHomeServicesPortalDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(projectDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        return config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                $"ConnectionStrings:DefaultConnection not found in {projectDir}'s appsettings.");
    }

    private static string FindHomeServicesPortalDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "HomeServicesPortal");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate HomeServicesPortal/appsettings.json by walking up from {AppContext.BaseDirectory}. " +
            "Run the tests from within the repo, or adjust FindHomeServicesPortalDirectory.");
    }
}

[CollectionDefinition("StatusWorkflow")]
public class StatusWorkflowCollection : ICollectionFixture<DatabaseFixture>
{
}

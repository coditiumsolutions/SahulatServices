using HomeServicesPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class GenderSyncService : IGenderSyncService
{
    private readonly AppDbContext _db;

    public GenderSyncService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SyncGenderAsync(int userUid, string? gender, CancellationToken cancellationToken = default)
    {
        await _db.Clients
            .Where(c => c.UserUid == userUid)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Gender, gender), cancellationToken);

        await _db.Providers
            .Where(p => p.UserUid == userUid)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Gender, gender), cancellationToken);
    }
}

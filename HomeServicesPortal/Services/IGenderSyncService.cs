namespace HomeServicesPortal.Services;

public interface IGenderSyncService
{
    /// <summary>
    /// Writes <paramref name="gender"/> to every Clients/Providers row owned by
    /// <paramref name="userUid"/>, so a dual-role account's Gender stays identical
    /// on both sides no matter which role's edit screen it was changed from.
    /// Composes into the caller's own SaveChanges/transaction — does not commit.
    /// </summary>
    Task SyncGenderAsync(int userUid, string? gender, CancellationToken cancellationToken = default);
}

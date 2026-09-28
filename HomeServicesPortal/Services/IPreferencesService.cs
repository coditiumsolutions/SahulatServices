namespace HomeServicesPortal.Services;

public interface IPreferencesService
{
    /// <summary>Returns true if the saved preference is 12-hour time display, false for 24-hour (default).</summary>
    Task<bool> GetUse12HourAsync(CancellationToken cancellationToken = default);

    Task SetUse12HourAsync(bool use12Hour, CancellationToken cancellationToken = default);
}

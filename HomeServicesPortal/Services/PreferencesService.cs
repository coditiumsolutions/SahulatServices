using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class PreferencesService : IPreferencesService
{
    private readonly AppDbContext _db;

    public PreferencesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> GetUse12HourAsync(CancellationToken cancellationToken = default)
    {
        var value = await _db.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == TimeFormatPreference.ConfigKey)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken);

        var use12Hour = value == TimeFormatPreference.TwelveHour;
        TimeFormatPreference.Set(value);
        return use12Hour;
    }

    public async Task SetUse12HourAsync(bool use12Hour, CancellationToken cancellationToken = default)
    {
        var configValue = use12Hour ? TimeFormatPreference.TwelveHour : TimeFormatPreference.TwentyFourHour;

        var entity = await _db.Configurations
            .FirstOrDefaultAsync(c => c.ConfigKey == TimeFormatPreference.ConfigKey, cancellationToken);

        if (entity == null)
        {
            _db.Configurations.Add(new ConfigurationEntry
            {
                ConfigKey = TimeFormatPreference.ConfigKey,
                ConfigValue = configValue,
                CreatedOn = DateTime.Now
            });
        }
        else
        {
            entity.ConfigValue = configValue;
        }

        await _db.SaveChangesAsync(cancellationToken);
        TimeFormatPreference.Set(configValue);
    }
}

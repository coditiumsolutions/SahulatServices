using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ConfigurationEntryService : IConfigurationEntryService
{
    private readonly AppDbContext _db;

    public ConfigurationEntryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ConfigurationListVm> GetListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 15;
        page = page < 1 ? 1 : page;

        var query = _db.Configurations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.ConfigKey.Contains(term) ||
                c.ConfigValue.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.ConfigKey)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConfigurationItemVm
            {
                Uid = c.Uid,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue,
                CreatedOn = c.CreatedOn
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.ValueCount = SplitValues(item.ConfigValue).Count;
        }

        return new ConfigurationListVm
        {
            Items = items,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ConfigurationDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Configurations
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ConfigurationDetailsVm
            {
                Uid = c.Uid,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue,
                CreatedOn = c.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return null;
        row.Values = SplitValues(row.ConfigValue);
        return row;
    }

    public async Task<ConfigurationFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Configurations
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ConfigurationFormVm
            {
                Uid = c.Uid,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConfigurationDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Configurations
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ConfigurationDeleteVm
            {
                Uid = c.Uid,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ConfigurationFormVm model,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(model.ConfigKey);
        var value = NormalizeValue(model.ConfigValue);

        if (string.IsNullOrWhiteSpace(key))
            return (false, "Config key is required.");
        if (string.IsNullOrWhiteSpace(value))
            return (false, "Config value is required.");

        var exists = await _db.Configurations
            .AnyAsync(c => c.ConfigKey == key, cancellationToken);
        if (exists)
            return (false, $"Config key '{key}' already exists.");

        _db.Configurations.Add(new ConfigurationEntry
        {
            ConfigKey = key,
            ConfigValue = value,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ConfigurationFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Uid == model.Uid, cancellationToken);
        if (entity == null)
            return (false, "Configuration not found.");

        var key = NormalizeKey(model.ConfigKey);
        var value = NormalizeValue(model.ConfigValue);

        if (string.IsNullOrWhiteSpace(key))
            return (false, "Config key is required.");
        if (string.IsNullOrWhiteSpace(value))
            return (false, "Config value is required.");

        var keyTaken = await _db.Configurations
            .AnyAsync(c => c.ConfigKey == key && c.Uid != model.Uid, cancellationToken);
        if (keyTaken)
            return (false, $"Config key '{key}' already exists.");

        entity.ConfigKey = key;
        entity.ConfigValue = value;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Uid == id, cancellationToken);
        if (entity == null)
            return (false, "Configuration not found.");

        _db.Configurations.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<IReadOnlyList<string>> GetValuesByKeyAsync(
        string configKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configKey))
            return Array.Empty<string>();

        var key = configKey.Trim();
        var raw = await _db.Configurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == key)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken);

        return SplitValues(raw);
    }

    private static string NormalizeKey(string? key) => (key ?? string.Empty).Trim();

    private static string NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var parts = SplitValues(value);
        return string.Join(", ", parts);
    }

    private static List<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        return value
            .Replace('\r', '\n')
            .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

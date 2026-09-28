using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ProviderServiceTitleService : IProviderServiceTitleService
{
    private readonly AppDbContext _db;
    private readonly IProviderCategoryService _providerCategories;

    public ProviderServiceTitleService(AppDbContext db, IProviderCategoryService providerCategories)
    {
        _db = db;
        _providerCategories = providerCategories;
    }

    public async Task<List<int>> GetServiceTitleUidsAsync(int providerUid, CancellationToken cancellationToken = default)
    {
        return await _db.ProviderServiceTitles
            .AsNoTracking()
            .Where(pt => pt.ProviderUid == providerUid)
            .OrderBy(pt => pt.ServiceTitleUid)
            .Select(pt => pt.ServiceTitleUid)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProviderServiceTitleItemDto>> GetServiceTitlesAsync(int providerUid, CancellationToken cancellationToken = default)
    {
        return await _db.ProviderServiceTitles
            .AsNoTracking()
            .Where(pt => pt.ProviderUid == providerUid)
            .OrderBy(pt => pt.ServiceTitle.CategoryUid)
            .ThenBy(pt => pt.ServiceTitle.Title)
            .Select(pt => new ProviderServiceTitleItemDto
            {
                ServiceTitleUid = pt.ServiceTitleUid,
                Title = pt.ServiceTitle.Title,
                CategoryUid = pt.ServiceTitle.CategoryUid,
                CategoryName = pt.ServiceTitle.Category.CategoryName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> SyncServiceTitlesAsync(
        int providerUid,
        List<int> serviceTitleUids,
        CancellationToken cancellationToken = default)
    {
        var distinctUids = (serviceTitleUids ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var providerExists = await _db.Providers.AnyAsync(p => p.Uid == providerUid, cancellationToken);
        if (!providerExists)
        {
            return (false, "Provider not found.");
        }

        if (distinctUids.Count == 0)
        {
            var existingRows = await _db.ProviderServiceTitles
                .Where(pt => pt.ProviderUid == providerUid)
                .ToListAsync(cancellationToken);
            if (existingRows.Count > 0)
            {
                _db.ProviderServiceTitles.RemoveRange(existingRows);
                await _db.SaveChangesAsync(cancellationToken);
            }
            return (true, null);
        }

        var titles = await _db.ServiceTitles
            .Where(t => distinctUids.Contains(t.Uid))
            .Select(t => new { t.Uid, t.Title, t.CategoryUid, t.IsActive })
            .ToListAsync(cancellationToken);

        if (titles.Count != distinctUids.Count)
        {
            return (false, "One or more selected service titles do not exist.");
        }

        var inactive = titles.FirstOrDefault(t => !t.IsActive);
        if (inactive != null)
        {
            return (false, $"Service title '{inactive.Title}' is not active.");
        }

        var providerCategoryUids = await _providerCategories.GetCategoryUidsAsync(providerUid, cancellationToken);
        var outOfCategory = titles.FirstOrDefault(t => !providerCategoryUids.Contains(t.CategoryUid));
        if (outOfCategory != null)
        {
            return (false, $"Service title '{outOfCategory.Title}' belongs to a category this provider is not assigned to.");
        }

        var existing = await _db.ProviderServiceTitles
            .Where(pt => pt.ProviderUid == providerUid)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(pt => !distinctUids.Contains(pt.ServiceTitleUid)).ToList();
        if (toRemove.Count > 0)
        {
            _db.ProviderServiceTitles.RemoveRange(toRemove);
        }

        var existingUids = existing.Select(pt => pt.ServiceTitleUid).ToHashSet();
        foreach (var titleUid in distinctUids)
        {
            if (!existingUids.Contains(titleUid))
            {
                _db.ProviderServiceTitles.Add(new ProviderServiceTitle
                {
                    ProviderUid = providerUid,
                    ServiceTitleUid = titleUid,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}

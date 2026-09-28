using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ProviderCategoryService : IProviderCategoryService
{
    private readonly AppDbContext _db;

    public ProviderCategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<int>> GetCategoryUidsAsync(int providerUid, CancellationToken cancellationToken = default)
    {
        return await _db.ProviderCategories
            .AsNoTracking()
            .Where(pc => pc.ProviderUid == providerUid)
            .OrderByDescending(pc => pc.IsPrimary)
            .ThenBy(pc => pc.CategoryUid)
            .Select(pc => pc.CategoryUid)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProviderCategoryItemDto>> GetCategoriesAsync(int providerUid, CancellationToken cancellationToken = default)
    {
        return await _db.ProviderCategories
            .AsNoTracking()
            .Where(pc => pc.ProviderUid == providerUid)
            .OrderByDescending(pc => pc.IsPrimary)
            .ThenBy(pc => pc.Category.CategoryName)
            .Select(pc => new ProviderCategoryItemDto
            {
                CategoryUid = pc.CategoryUid,
                CategoryName = pc.Category.CategoryName,
                IsPrimary = pc.IsPrimary
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> SyncCategoriesAsync(
        int providerUid,
        List<int> categoryUids,
        int primaryCategoryUid,
        CancellationToken cancellationToken = default)
    {
        var distinctUids = (categoryUids ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (distinctUids.Count == 0)
        {
            return (false, "Select at least one category.");
        }

        if (!distinctUids.Contains(primaryCategoryUid))
        {
            return (false, "The primary category must be one of the selected categories.");
        }

        var providerExists = await _db.Providers.AnyAsync(p => p.Uid == providerUid, cancellationToken);
        if (!providerExists)
        {
            return (false, "Provider not found.");
        }

        var validCategoryCount = await _db.ServiceCategories
            .CountAsync(c => distinctUids.Contains(c.Uid), cancellationToken);
        if (validCategoryCount != distinctUids.Count)
        {
            return (false, "One or more selected categories do not exist.");
        }

        var existing = await _db.ProviderCategories
            .Where(pc => pc.ProviderUid == providerUid)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(pc => !distinctUids.Contains(pc.CategoryUid)).ToList();
        if (toRemove.Count > 0)
        {
            _db.ProviderCategories.RemoveRange(toRemove);
        }

        var existingUids = existing.Select(pc => pc.CategoryUid).ToHashSet();
        foreach (var categoryUid in distinctUids)
        {
            if (!existingUids.Contains(categoryUid))
            {
                _db.ProviderCategories.Add(new ProviderCategory
                {
                    ProviderUid = providerUid,
                    CategoryUid = categoryUid,
                    IsPrimary = categoryUid == primaryCategoryUid,
                    CreatedOn = DateTime.Now
                });
            }
        }

        foreach (var row in existing)
        {
            if (distinctUids.Contains(row.CategoryUid))
            {
                row.IsPrimary = row.CategoryUid == primaryCategoryUid;
            }
        }

        await _db.Providers
            .Where(p => p.Uid == providerUid)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CategoryUid, primaryCategoryUid), cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}

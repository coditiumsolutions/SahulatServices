using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ProviderDetailService : IProviderDetailService
{
    private const string CitiesConfigKey = "Cities";

    private readonly AppDbContext _db;
    private readonly IConfigurationEntryService _configurations;

    public ProviderDetailService(AppDbContext db, IConfigurationEntryService configurations)
    {
        _db = db;
        _configurations = configurations;
    }

    public async Task<(bool Success, string? Error, ProviderDetailApiDto? Data)> GetProviderDetailAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == providerUid)
            .Select(p => new ProviderDetailApiDto
            {
                Uid = p.Uid,
                UserUid = p.UserUid,
                MobileNo = p.MobileNo,
                FullName = p.FullName,
                Cnic = p.Cnic,
                Gender = p.Gender,
                ExperienceYears = p.ExperienceYears,
                Description = p.Description,
                City = p.City,
                IsVerified = p.IsVerified,
                AverageRating = p.AverageRating,
                TotalReviews = p.TotalReviews,
                TotalJobsCompleted = p.TotalJobsCompleted,
                IsAvailable = p.IsAvailable,
                AvailableTiming = p.AvailableTiming,
                CategoryId = p.CategoryUid,
                CategoryName = p.Category.CategoryName,
                CreatedOn = p.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.", null);
        }

        return (true, null, provider);
    }

    public async Task<(bool Success, string? Error, ProviderDetailApiDto? Data)> UpdateProviderDetailAsync(
        UpdateProviderDetailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .FirstOrDefaultAsync(p => p.Uid == request.ProviderUid, cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.", null);
        }

        var (categoryId, categoryError) = await ResolveCategoryAsync(
            request.CategoryId,
            request.CategoryName,
            cancellationToken);

        if (categoryError != null)
        {
            return (false, categoryError, null);
        }

        var city = request.City?.Trim();
        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityOptions = await _configurations.GetValuesByKeyAsync(CitiesConfigKey, cancellationToken);
            if (!cityOptions.Any(c => c.Equals(city, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"City must be one of the configured options: {string.Join(", ", cityOptions)}.", null);
            }

            provider.City = city;
        }

        provider.FullName = request.FullName.Trim();
        provider.Cnic = request.CNIC.Trim();
        provider.Gender = request.Gender?.Trim();
        provider.ExperienceYears = request.ExperienceYears ?? 0;
        provider.Description = request.Description?.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        // Keep the deprecated CategoryUid scalar and the ProviderCategories junction table in
        // sync: this endpoint only lets the provider change their primary category, so add/mark
        // it as primary here without disturbing any additional categories they hold via the
        // separate PUT /api/providers/{id}/categories management endpoint.
        await SyncPrimaryCategoryAsync(provider.Uid, categoryId!.Value, cancellationToken);

        return await GetProviderDetailAsync(provider.Uid, cancellationToken);
    }

    private async Task SyncPrimaryCategoryAsync(int providerUid, int categoryUid, CancellationToken cancellationToken)
    {
        await _db.Providers
            .Where(p => p.Uid == providerUid)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CategoryUid, categoryUid), cancellationToken);

        await _db.ProviderCategories
            .Where(pc => pc.ProviderUid == providerUid && pc.IsPrimary && pc.CategoryUid != categoryUid)
            .ExecuteUpdateAsync(s => s.SetProperty(pc => pc.IsPrimary, false), cancellationToken);

        var existingRow = await _db.ProviderCategories
            .FirstOrDefaultAsync(pc => pc.ProviderUid == providerUid && pc.CategoryUid == categoryUid, cancellationToken);

        if (existingRow != null)
        {
            if (!existingRow.IsPrimary)
            {
                existingRow.IsPrimary = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            _db.ProviderCategories.Add(new Entities.ProviderCategory
            {
                ProviderUid = providerUid,
                CategoryUid = categoryUid,
                IsPrimary = true,
                CreatedOn = DateTime.Now
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(int? CategoryId, string? Error)> ResolveCategoryAsync(
        int? categoryId,
        string? categoryName,
        CancellationToken cancellationToken)
    {
        if (categoryId.HasValue)
        {
            var exists = await _db.ServiceCategories
                .AsNoTracking()
                .AnyAsync(c => c.Uid == categoryId.Value && c.IsActive, cancellationToken);

            if (!exists)
            {
                return (null, "Invalid or inactive service category id.");
            }

            return (categoryId.Value, null);
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var term = categoryName.Trim();
            var byName = await _db.ServiceCategories
                .AsNoTracking()
                .Where(c => c.IsActive && c.CategoryName.ToLower() == term.ToLower())
                .Select(c => c.Uid)
                .FirstOrDefaultAsync(cancellationToken);

            if (byName == 0)
            {
                return (null, $"Service category '{term}' was not found.");
            }

            return (byName, null);
        }

        return (null, "CategoryId or CategoryName is required.");
    }
}

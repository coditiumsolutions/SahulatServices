using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ProviderDetailService : IProviderDetailService
{
    private readonly AppDbContext _db;

    public ProviderDetailService(AppDbContext db)
    {
        _db = db;
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
                MobileNo = p.User.MobileNo,
                FullName = p.FullName,
                Cnic = p.Cnic,
                Gender = p.Gender,
                ExperienceYears = p.ExperienceYears,
                Description = p.Description,
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

        provider.FullName = request.FullName.Trim();
        provider.Cnic = request.CNIC.Trim();
        provider.Gender = request.Gender?.Trim();
        provider.ExperienceYears = request.ExperienceYears ?? 0;
        provider.Description = request.Description?.Trim();
        provider.CategoryUid = categoryId!.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetProviderDetailAsync(provider.Uid, cancellationToken);
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

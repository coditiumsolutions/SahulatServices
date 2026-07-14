using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using EntityServiceCategory = HomeServicesPortal.Entities.ServiceCategory;

namespace HomeServicesPortal.Services;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly AppDbContext _db;

    public ServiceCategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceCategoryApiDto>> GetActiveCategoriesForApiAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new ServiceCategoryApiDto
            {
                Id = c.Uid,
                Name = c.CategoryName,
                Description = c.Description,
                CreatedOn = c.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCategoryApiDto?> GetActiveCategoryForApiAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Uid == id && c.IsActive)
            .Select(c => new ServiceCategoryApiDto
            {
                Id = c.Uid,
                Name = c.CategoryName,
                Description = c.Description,
                CreatedOn = c.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceCategoryListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var query = _db.ServiceCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.CategoryName.Contains(term) ||
                (c.Description != null && c.Description.Contains(term)));
        }

        query = sort switch
        {
            "date" => sortDir == "desc"
                ? query.OrderByDescending(c => c.CreatedOn)
                : query.OrderBy(c => c.CreatedOn),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(c => c.IsActive)
                : query.OrderBy(c => c.IsActive),
            _ => sortDir == "desc"
                ? query.OrderByDescending(c => c.CategoryName)
                : query.OrderBy(c => c.CategoryName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ServiceCategoryItemVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedOn = c.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new ServiceCategoryListVm
        {
            Items = items,
            Search = search,
            Sort = sort,
            SortDir = sortDir,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ServiceCategoryDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ServiceCategoryDetailsVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedOn = c.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceCategoryFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ServiceCategoryFormVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceCategoryDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ServiceCategoryDeleteVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                Description = c.Description
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default)
    {
        var name = model.CategoryName.Trim();
        var exists = await _db.ServiceCategories
            .AnyAsync(c => c.CategoryName == name, cancellationToken);

        if (exists)
        {
            return (false, "A category with this name already exists.");
        }

        _db.ServiceCategories.Add(new EntityServiceCategory
        {
            CategoryName = name,
            Description = model.Description?.Trim(),
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Uid == model.Uid, cancellationToken);

        if (entity == null)
        {
            return (false, "Category not found.");
        }

        var name = model.CategoryName.Trim();
        var duplicate = await _db.ServiceCategories
            .AnyAsync(c => c.CategoryName == name && c.Uid != model.Uid, cancellationToken);

        if (duplicate)
        {
            return (false, "A category with this name already exists.");
        }

        entity.CategoryName = name;
        entity.Description = model.Description?.Trim();
        entity.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceCategories
            .FirstOrDefaultAsync(c => c.Uid == id, cancellationToken);

        if (entity == null)
        {
            return (false, "Category not found.");
        }

        var inUse = await _db.Providers.AnyAsync(p => p.CategoryUid == id, cancellationToken)
                    || await _db.CustomerServiceRequests.AnyAsync(r => r.CategoryUid == id, cancellationToken);

        if (inUse)
        {
            return (false, "Cannot delete this category because it is linked to providers or service requests.");
        }

        _db.ServiceCategories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}

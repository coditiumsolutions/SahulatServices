using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        int? serviceUid = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Service.IsActive);

        if (serviceUid.HasValue)
        {
            query = query.Where(c => c.ServiceUid == serviceUid.Value);
        }

        return await query
            .OrderBy(c => c.Service.DisplayOrder)
            .ThenBy(c => c.CategoryName)
            .Select(c => new ServiceCategoryApiDto
            {
                Id = c.Uid,
                ServiceId = c.ServiceUid,
                ServiceName = c.Service.ServiceName,
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
            .Where(c => c.Uid == id && c.IsActive && c.Service.IsActive)
            .Select(c => new ServiceCategoryApiDto
            {
                Id = c.Uid,
                ServiceId = c.ServiceUid,
                ServiceName = c.Service.ServiceName,
                Name = c.CategoryName,
                Description = c.Description,
                CreatedOn = c.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetServiceOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.ServiceName)
            .Select(s => new SelectListItem
            {
                Value = s.Uid.ToString(),
                Text = s.ServiceName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCategoryFormVm> PopulateFormAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Services = await GetServiceOptionsAsync(cancellationToken);
        return model;
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

        var query =
            from c in _db.ServiceCategories.AsNoTracking()
            join s in _db.Services.AsNoTracking() on c.ServiceUid equals s.Uid
            select new { Category = c, Service = s };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Category.CategoryName.Contains(term)
                || (x.Category.Description != null && x.Category.Description.Contains(term))
                || x.Service.ServiceName.Contains(term));
        }

        query = sort switch
        {
            "service" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Service.ServiceName).ThenBy(x => x.Category.CategoryName)
                : query.OrderBy(x => x.Service.ServiceName).ThenBy(x => x.Category.CategoryName),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Category.CreatedOn)
                : query.OrderBy(x => x.Category.CreatedOn),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Category.IsActive)
                : query.OrderBy(x => x.Category.IsActive),
            _ => sortDir == "desc"
                ? query.OrderByDescending(x => x.Category.CategoryName)
                : query.OrderBy(x => x.Category.CategoryName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceCategoryItemVm
            {
                Uid = x.Category.Uid,
                ServiceUid = x.Category.ServiceUid,
                ServiceName = x.Service.ServiceName,
                CategoryName = x.Category.CategoryName,
                Description = x.Category.Description,
                IsActive = x.Category.IsActive,
                CreatedOn = x.Category.CreatedOn
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
        return await (
            from c in _db.ServiceCategories.AsNoTracking()
            join s in _db.Services.AsNoTracking() on c.ServiceUid equals s.Uid
            where c.Uid == id
            select new ServiceCategoryDetailsVm
            {
                Uid = c.Uid,
                ServiceUid = c.ServiceUid,
                ServiceName = s.ServiceName,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedOn = c.CreatedOn
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceCategoryFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new ServiceCategoryFormVm
            {
                Uid = c.Uid,
                ServiceUid = c.ServiceUid,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (model == null) return null;
        return await PopulateFormAsync(model, cancellationToken);
    }

    public async Task<ServiceCategoryDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await (
            from c in _db.ServiceCategories.AsNoTracking()
            join s in _db.Services.AsNoTracking() on c.ServiceUid equals s.Uid
            where c.Uid == id
            select new ServiceCategoryDeleteVm
            {
                Uid = c.Uid,
                ServiceUid = c.ServiceUid,
                ServiceName = s.ServiceName,
                CategoryName = c.CategoryName,
                Description = c.Description
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default)
    {
        if (model.ServiceUid <= 0
            || !await _db.Services.AnyAsync(s => s.Uid == model.ServiceUid && s.IsActive, cancellationToken))
        {
            return (false, "A valid parent service is required.");
        }

        var name = model.CategoryName.Trim();
        var exists = await _db.ServiceCategories
            .AnyAsync(c => c.CategoryName == name && c.ServiceUid == model.ServiceUid, cancellationToken);

        if (exists)
        {
            return (false, "A category with this name already exists under the selected service.");
        }

        _db.ServiceCategories.Add(new EntityServiceCategory
        {
            ServiceUid = model.ServiceUid,
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

        if (model.ServiceUid <= 0
            || !await _db.Services.AnyAsync(s => s.Uid == model.ServiceUid && s.IsActive, cancellationToken))
        {
            return (false, "A valid parent service is required.");
        }

        var name = model.CategoryName.Trim();
        var duplicate = await _db.ServiceCategories
            .AnyAsync(
                c => c.CategoryName == name
                     && c.ServiceUid == model.ServiceUid
                     && c.Uid != model.Uid,
                cancellationToken);

        if (duplicate)
        {
            return (false, "A category with this name already exists under the selected service.");
        }

        entity.ServiceUid = model.ServiceUid;
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

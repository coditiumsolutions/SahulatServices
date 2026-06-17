using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly IRepository<ServiceCategory> _repo;

    public ServiceCategoryService(IRepository<ServiceCategory> repo)
    {
        _repo = repo;
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

        var query = _repo.Query();

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
                IsActive = c.IsActive ?? true,
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
        return await _repo.Query()
            .Where(c => c.Uid == id)
            .Select(c => new ServiceCategoryDetailsVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive ?? true,
                CreatedOn = c.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceCategoryFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return new ServiceCategoryFormVm
        {
            Uid = entity.Uid,
            CategoryName = entity.CategoryName,
            Description = entity.Description,
            IsActive = entity.IsActive ?? true
        };
    }

    public async Task<ServiceCategoryDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repo.Query()
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
        var exists = await _repo.Query()
            .AnyAsync(c => c.CategoryName == model.CategoryName, cancellationToken);

        if (exists)
        {
            return (false, "A category with this name already exists.");
        }

        var entity = new ServiceCategory
        {
            CategoryName = model.CategoryName.Trim(),
            Description = model.Description?.Trim(),
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now
        };

        await _repo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Category not found.");
        }

        var duplicate = await _repo.Query()
            .AnyAsync(c => c.CategoryName == model.CategoryName && c.Uid != model.Uid, cancellationToken);

        if (duplicate)
        {
            return (false, "A category with this name already exists.");
        }

        entity.CategoryName = model.CategoryName.Trim();
        entity.Description = model.Description?.Trim();
        entity.IsActive = model.IsActive;

        await _repo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (false, "Category not found.");
        }

        try
        {
            await _repo.DeleteAsync(entity, cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this category because it is linked to providers or service requests.");
        }
    }
}

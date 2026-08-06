using HomeServicesPortal.Data;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using EntityService = HomeServicesPortal.Entities.Service;

namespace HomeServicesPortal.Services;

public class ServiceService : IServiceService
{
    private readonly AppDbContext _db;

    public ServiceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceListVm> GetListAsync(
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

        var query = _db.Services.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.ServiceName.Contains(term)
                || (s.Description != null && s.Description.Contains(term)));
        }

        query = sort switch
        {
            "order" => sortDir == "desc"
                ? query.OrderByDescending(s => s.DisplayOrder)
                : query.OrderBy(s => s.DisplayOrder),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(s => s.CreatedOn)
                : query.OrderBy(s => s.CreatedOn),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(s => s.IsActive)
                : query.OrderBy(s => s.IsActive),
            _ => sortDir == "desc"
                ? query.OrderByDescending(s => s.ServiceName)
                : query.OrderBy(s => s.ServiceName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceItemVm
            {
                Uid = s.Uid,
                ServiceName = s.ServiceName,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive,
                CreatedOn = s.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new ServiceListVm
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

    public async Task<ServiceDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.Uid == id)
            .Select(s => new ServiceDetailsVm
            {
                Uid = s.Uid,
                ServiceName = s.ServiceName,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive,
                CreatedOn = s.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.Uid == id)
            .Select(s => new ServiceFormVm
            {
                Uid = s.Uid,
                ServiceName = s.ServiceName,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.Uid == id)
            .Select(s => new ServiceDeleteVm
            {
                Uid = s.Uid,
                ServiceName = s.ServiceName,
                Description = s.Description
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceFormVm model,
        CancellationToken cancellationToken = default)
    {
        var name = model.ServiceName.Trim();
        var exists = await _db.Services.AnyAsync(s => s.ServiceName == name, cancellationToken);
        if (exists)
        {
            return (false, "A service with this name already exists.");
        }

        _db.Services.Add(new EntityService
        {
            ServiceName = name,
            Description = model.Description?.Trim(),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Services.FirstOrDefaultAsync(s => s.Uid == model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Service not found.");
        }

        var name = model.ServiceName.Trim();
        var duplicate = await _db.Services
            .AnyAsync(s => s.ServiceName == name && s.Uid != model.Uid, cancellationToken);
        if (duplicate)
        {
            return (false, "A service with this name already exists.");
        }

        entity.ServiceName = name;
        entity.Description = model.Description?.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Services.FirstOrDefaultAsync(s => s.Uid == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Service not found.");
        }

        var inUse = await _db.ServiceCategories.AnyAsync(c => c.ServiceUid == id, cancellationToken);
        if (inUse)
        {
            return (false, "Cannot delete this service because it has categories linked to it.");
        }

        _db.Services.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}

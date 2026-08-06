using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EntityServiceTitle = HomeServicesPortal.Entities.ServiceTitle;

namespace HomeServicesPortal.Services;

public class ServiceTitleService : IServiceTitleService
{
    private readonly AppDbContext _db;

    public ServiceTitleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceTitleApiDto>> GetActiveTitlesForApiAsync(
        int categoryUid,
        CancellationToken cancellationToken = default)
    {
        return await _db.ServiceTitles
            .AsNoTracking()
            .Where(t => t.IsActive && t.CategoryUid == categoryUid && t.Category.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new ServiceTitleApiDto
            {
                Id = t.Uid,
                CategoryId = t.CategoryUid,
                CategoryName = t.Category.CategoryName,
                Title = t.Title,
                Description = t.Description,
                DisplayOrder = t.DisplayOrder,
                CreatedOn = t.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceTitleApiDto?> GetActiveTitleForApiAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.ServiceTitles
            .AsNoTracking()
            .Where(t => t.Uid == id && t.IsActive && t.Category.IsActive)
            .Select(t => new ServiceTitleApiDto
            {
                Id = t.Uid,
                CategoryId = t.CategoryUid,
                CategoryName = t.Category.CategoryName,
                Title = t.Title,
                Description = t.Description,
                DisplayOrder = t.DisplayOrder,
                CreatedOn = t.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.CategoryName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceTitleFormVm> PopulateFormAsync(
        ServiceTitleFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Categories = await GetCategoryOptionsAsync(cancellationToken);
        return model;
    }

    public async Task<ServiceTitleListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "title" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var query =
            from t in _db.ServiceTitles.AsNoTracking()
            join c in _db.ServiceCategories.AsNoTracking() on t.CategoryUid equals c.Uid
            select new { Title = t, Category = c };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Title.Title.Contains(term)
                || (x.Title.Description != null && x.Title.Description.Contains(term))
                || x.Category.CategoryName.Contains(term));
        }

        query = sort switch
        {
            "category" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Category.CategoryName).ThenBy(x => x.Title.Title)
                : query.OrderBy(x => x.Category.CategoryName).ThenBy(x => x.Title.Title),
            "order" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Title.DisplayOrder)
                : query.OrderBy(x => x.Title.DisplayOrder),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Title.CreatedOn)
                : query.OrderBy(x => x.Title.CreatedOn),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Title.IsActive)
                : query.OrderBy(x => x.Title.IsActive),
            _ => sortDir == "desc"
                ? query.OrderByDescending(x => x.Title.Title)
                : query.OrderBy(x => x.Title.Title)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceTitleItemVm
            {
                Uid = x.Title.Uid,
                CategoryUid = x.Title.CategoryUid,
                CategoryName = x.Category.CategoryName,
                Title = x.Title.Title,
                Description = x.Title.Description,
                DisplayOrder = x.Title.DisplayOrder,
                IsActive = x.Title.IsActive,
                CreatedOn = x.Title.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new ServiceTitleListVm
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

    public async Task<ServiceTitleDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await (
            from t in _db.ServiceTitles.AsNoTracking()
            join c in _db.ServiceCategories.AsNoTracking() on t.CategoryUid equals c.Uid
            where t.Uid == id
            select new ServiceTitleDetailsVm
            {
                Uid = t.Uid,
                CategoryUid = t.CategoryUid,
                CategoryName = c.CategoryName,
                Title = t.Title,
                Description = t.Description,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                CreatedOn = t.CreatedOn
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceTitleFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await _db.ServiceTitles
            .AsNoTracking()
            .Where(t => t.Uid == id)
            .Select(t => new ServiceTitleFormVm
            {
                Uid = t.Uid,
                CategoryUid = t.CategoryUid,
                Title = t.Title,
                Description = t.Description,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (model == null) return null;
        return await PopulateFormAsync(model, cancellationToken);
    }

    public async Task<ServiceTitleDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await (
            from t in _db.ServiceTitles.AsNoTracking()
            join c in _db.ServiceCategories.AsNoTracking() on t.CategoryUid equals c.Uid
            where t.Uid == id
            select new ServiceTitleDeleteVm
            {
                Uid = t.Uid,
                CategoryUid = t.CategoryUid,
                CategoryName = c.CategoryName,
                Title = t.Title,
                Description = t.Description
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceTitleFormVm model,
        CancellationToken cancellationToken = default)
    {
        if (model.CategoryUid <= 0
            || !await _db.ServiceCategories.AnyAsync(c => c.Uid == model.CategoryUid && c.IsActive, cancellationToken))
        {
            return (false, "A valid parent category is required.");
        }

        var title = model.Title.Trim();
        var exists = await _db.ServiceTitles
            .AnyAsync(t => t.Title == title && t.CategoryUid == model.CategoryUid, cancellationToken);

        if (exists)
        {
            return (false, "A title with this name already exists under the selected category.");
        }

        _db.ServiceTitles.Add(new EntityServiceTitle
        {
            CategoryUid = model.CategoryUid,
            Title = title,
            Description = model.Description?.Trim(),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceTitleFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceTitles
            .FirstOrDefaultAsync(t => t.Uid == model.Uid, cancellationToken);

        if (entity == null)
        {
            return (false, "Title not found.");
        }

        if (model.CategoryUid <= 0
            || !await _db.ServiceCategories.AnyAsync(c => c.Uid == model.CategoryUid && c.IsActive, cancellationToken))
        {
            return (false, "A valid parent category is required.");
        }

        var title = model.Title.Trim();
        var duplicate = await _db.ServiceTitles
            .AnyAsync(
                t => t.Title == title
                     && t.CategoryUid == model.CategoryUid
                     && t.Uid != model.Uid,
                cancellationToken);

        if (duplicate)
        {
            return (false, "A title with this name already exists under the selected category.");
        }

        entity.CategoryUid = model.CategoryUid;
        entity.Title = title;
        entity.Description = model.Description?.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceTitles
            .FirstOrDefaultAsync(t => t.Uid == id, cancellationToken);

        if (entity == null)
        {
            return (false, "Title not found.");
        }

        _db.ServiceTitles.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}

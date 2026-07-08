using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Services;

public class ProviderLocationService : IProviderLocationService
{
    private readonly IRepository<ProviderLocation> _locationRepo;
    private readonly IRepository<ProviderProfile> _providerRepo;

    public ProviderLocationService(
        IRepository<ProviderLocation> locationRepo,
        IRepository<ProviderProfile> providerRepo)
    {
        _locationRepo = locationRepo;
        _providerRepo = providerRepo;
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _providerRepo.Query()
            .Where(p => p.UserU.IsActive != false && p.UserU.UserType == UserTypeConstants.Provider)
            .OrderBy(p => p.UserU.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.UserU.FullName ?? "?"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderLocationListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "provider" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var providerMap = await _providerRepo.Query()
            .Select(p => new { p.Uid, FullName = p.UserU.FullName ?? "?" })
            .ToDictionaryAsync(p => p.Uid, p => p.FullName, cancellationToken);

        var query = _locationRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var matchingProviderIds = providerMap
                .Where(p => p.Value.ToLowerInvariant().Contains(term))
                .Select(p => p.Key)
                .ToList();

            query = query.Where(l =>
                matchingProviderIds.Contains(l.ProviderUid) ||
                (l.Latitude != null && l.Latitude.ToString()!.Contains(term)) ||
                (l.Longitude != null && l.Longitude.ToString()!.Contains(term)));
        }

        var locations = await query.ToListAsync(cancellationToken);

        IEnumerable<ProviderLocationItemVm> projected = locations.Select(l => new ProviderLocationItemVm
        {
            Uid = l.Uid,
            ProviderUid = l.ProviderUid,
            ProviderName = providerMap.GetValueOrDefault(l.ProviderUid, "?"),
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            LastUpdated = l.LastUpdated
        });

        projected = sort switch
        {
            "latitude" => sortDir == "desc"
                ? projected.OrderByDescending(x => x.Latitude)
                : projected.OrderBy(x => x.Latitude),
            "longitude" => sortDir == "desc"
                ? projected.OrderByDescending(x => x.Longitude)
                : projected.OrderBy(x => x.Longitude),
            "date" => sortDir == "desc"
                ? projected.OrderByDescending(x => x.LastUpdated)
                : projected.OrderBy(x => x.LastUpdated),
            _ => sortDir == "desc"
                ? projected.OrderByDescending(x => x.ProviderName)
                : projected.OrderBy(x => x.ProviderName)
        };

        var list = projected.ToList();
        var totalCount = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new ProviderLocationListVm
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

    public async Task<ProviderLocationDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepo.Query()
            .FirstOrDefaultAsync(l => l.Uid == id, cancellationToken);

        if (location == null) return null;

        var providerName = await _providerRepo.Query()
            .Where(p => p.Uid == location.ProviderUid)
            .Select(p => p.UserU.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "?";

        return MapDetails(location, providerName);
    }

    public async Task<ProviderLocationFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepo.GetByIdAsync(id, cancellationToken);
        if (location == null) return null;

        return new ProviderLocationFormVm
        {
            Uid = location.Uid,
            ProviderUid = location.ProviderUid,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Providers = await GetProviderOptionsAsync(cancellationToken)
        };
    }

    public async Task<ProviderLocationDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepo.Query()
            .FirstOrDefaultAsync(l => l.Uid == id, cancellationToken);

        if (location == null) return null;

        var providerName = await _providerRepo.Query()
            .Where(p => p.Uid == location.ProviderUid)
            .Select(p => p.UserU.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "?";

        return new ProviderLocationDeleteVm
        {
            Uid = location.Uid,
            ProviderName = providerName,
            Latitude = location.Latitude,
            Longitude = location.Longitude
        };
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ProviderLocationFormVm model,
        CancellationToken cancellationToken = default)
    {
        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);

        if (!providerExists)
        {
            return (false, "Selected provider does not exist.");
        }

        var entity = new ProviderLocation
        {
            ProviderUid = model.ProviderUid,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            LastUpdated = DateTime.Now
        };

        await _locationRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ProviderLocationFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _locationRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Location not found.");
        }

        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);

        if (!providerExists)
        {
            return (false, "Selected provider does not exist.");
        }

        entity.ProviderUid = model.ProviderUid;
        entity.Latitude = model.Latitude;
        entity.Longitude = model.Longitude;
        entity.LastUpdated = DateTime.Now;

        await _locationRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _locationRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (false, "Location not found.");
        }

        await _locationRepo.DeleteAsync(entity, cancellationToken);
        return (true, null);
    }

    private static ProviderLocationDetailsVm MapDetails(ProviderLocation location, string providerName)
    {
        return new ProviderLocationDetailsVm
        {
            Uid = location.Uid,
            ProviderUid = location.ProviderUid,
            ProviderName = providerName,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            LastUpdated = location.LastUpdated
        };
    }
}

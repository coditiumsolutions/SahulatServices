using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using ServiceProviderEntity = HomeServicesPortal.Models.Entities.ServiceProvider;

namespace HomeServicesPortal.Services;

public class ProviderAvailabilityService : IProviderAvailabilityService
{
    private readonly IRepository<ProviderAvailability> _availabilityRepo;
    private readonly IRepository<ServiceProviderEntity> _providerRepo;

    public ProviderAvailabilityService(
        IRepository<ProviderAvailability> availabilityRepo,
        IRepository<ServiceProviderEntity> providerRepo)
    {
        _availabilityRepo = availabilityRepo;
        _providerRepo = providerRepo;
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _providerRepo.Query()
            .Where(p => p.IsActive != false)
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.FullName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderAvailabilityListVm> GetListAsync(
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
            .Select(p => new { p.Uid, p.FullName })
            .ToDictionaryAsync(p => p.Uid, p => p.FullName, cancellationToken);

        var query = _availabilityRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var matchingProviderIds = providerMap
                .Where(p => p.Value.ToLowerInvariant().Contains(term))
                .Select(p => p.Key)
                .ToList();

            query = query.Where(a =>
                matchingProviderIds.Contains(a.ProviderUid) ||
                (term == "online" && a.IsOnline == true) ||
                (term == "offline" && a.IsOnline != true));
        }

        var records = await query.ToListAsync(cancellationToken);

        IEnumerable<ProviderAvailabilityItemVm> projected = records.Select(a => new ProviderAvailabilityItemVm
        {
            Uid = a.Uid,
            ProviderUid = a.ProviderUid,
            ProviderName = providerMap.GetValueOrDefault(a.ProviderUid, "—"),
            IsOnline = a.IsOnline,
            AvailableFrom = a.AvailableFrom,
            AvailableTo = a.AvailableTo
        });

        projected = sort switch
        {
            "online" => sortDir == "desc"
                ? projected.OrderByDescending(x => x.IsOnline)
                : projected.OrderBy(x => x.IsOnline),
            "from" => sortDir == "desc"
                ? projected.OrderByDescending(x => x.AvailableFrom)
                : projected.OrderBy(x => x.AvailableFrom),
            "to" => sortDir == "desc"
                ? projected.OrderByDescending(x => x.AvailableTo)
                : projected.OrderBy(x => x.AvailableTo),
            _ => sortDir == "desc"
                ? projected.OrderByDescending(x => x.ProviderName)
                : projected.OrderBy(x => x.ProviderName)
        };

        var list = projected.ToList();
        var totalCount = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new ProviderAvailabilityListVm
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

    public async Task<ProviderAvailabilityDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _availabilityRepo.Query()
            .FirstOrDefaultAsync(a => a.Uid == id, cancellationToken);

        if (record == null) return null;

        var providerName = await _providerRepo.Query()
            .Where(p => p.Uid == record.ProviderUid)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        return MapDetails(record, providerName);
    }

    public async Task<ProviderAvailabilityFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _availabilityRepo.GetByIdAsync(id, cancellationToken);
        if (record == null) return null;

        return new ProviderAvailabilityFormVm
        {
            Uid = record.Uid,
            ProviderUid = record.ProviderUid,
            IsOnline = record.IsOnline ?? false,
            AvailableFrom = record.AvailableFrom,
            AvailableTo = record.AvailableTo,
            Providers = await GetProviderOptionsAsync(cancellationToken)
        };
    }

    public async Task<ProviderAvailabilityDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _availabilityRepo.Query()
            .FirstOrDefaultAsync(a => a.Uid == id, cancellationToken);

        if (record == null) return null;

        var providerName = await _providerRepo.Query()
            .Where(p => p.Uid == record.ProviderUid)
            .Select(p => p.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        return new ProviderAvailabilityDeleteVm
        {
            Uid = record.Uid,
            ProviderName = providerName,
            IsOnline = record.IsOnline,
            AvailableFrom = record.AvailableFrom,
            AvailableTo = record.AvailableTo
        };
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ProviderAvailabilityFormVm model,
        CancellationToken cancellationToken = default)
    {
        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);

        if (!providerExists)
        {
            return (false, "Selected provider does not exist.");
        }

        var entity = new ProviderAvailability
        {
            ProviderUid = model.ProviderUid,
            IsOnline = model.IsOnline,
            AvailableFrom = model.AvailableFrom,
            AvailableTo = model.AvailableTo
        };

        await _availabilityRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ProviderAvailabilityFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _availabilityRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Availability record not found.");
        }

        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);

        if (!providerExists)
        {
            return (false, "Selected provider does not exist.");
        }

        entity.ProviderUid = model.ProviderUid;
        entity.IsOnline = model.IsOnline;
        entity.AvailableFrom = model.AvailableFrom;
        entity.AvailableTo = model.AvailableTo;

        await _availabilityRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _availabilityRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (false, "Availability record not found.");
        }

        await _availabilityRepo.DeleteAsync(entity, cancellationToken);
        return (true, null);
    }

    private static ProviderAvailabilityDetailsVm MapDetails(ProviderAvailability record, string providerName)
    {
        return new ProviderAvailabilityDetailsVm
        {
            Uid = record.Uid,
            ProviderUid = record.ProviderUid,
            ProviderName = providerName,
            IsOnline = record.IsOnline,
            AvailableFrom = record.AvailableFrom,
            AvailableTo = record.AvailableTo
        };
    }
}

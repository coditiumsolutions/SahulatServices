using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Services;

public class ProviderQuoteService : IProviderQuoteService
{
    private readonly IRepository<ProviderQuote> _quoteRepo;
    private readonly IRepository<ServiceRequest> _requestRepo;
    private readonly IRepository<ProviderProfile> _providerRepo;

    public ProviderQuoteService(
        IRepository<ProviderQuote> quoteRepo,
        IRepository<ServiceRequest> requestRepo,
        IRepository<ProviderProfile> providerRepo)
    {
        _quoteRepo = quoteRepo;
        _requestRepo = requestRepo;
        _providerRepo = providerRepo;
    }

    public async Task<List<SelectListItem>> GetRequestOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _requestRepo.Query()
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new SelectListItem
            {
                Value = r.Uid.ToString(),
                Text = $"#{r.Uid} - {r.CustomerU.FullName} ({r.CategoryU.CategoryName})"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _providerRepo.Query()
            .Where(p => p.UserU.IsActive != false && p.UserU.UserType == UserTypeConstants.Provider)
            .OrderBy(p => p.UserU.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.UserU.FullName ?? "ù"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderQuoteListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "date" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var query = _quoteRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(q =>
                q.RequestU.CustomerU.FullName.Contains(term) ||
                q.RequestU.CategoryU.CategoryName.Contains(term) ||
                q.ProviderU.UserU.FullName.Contains(term) ||
                (q.Remarks != null && q.Remarks.Contains(term)));
        }

        query = sort switch
        {
            "request" => sortDir == "desc"
                ? query.OrderByDescending(q => q.RequestUid)
                : query.OrderBy(q => q.RequestUid),
            "provider" => sortDir == "desc"
                ? query.OrderByDescending(q => q.ProviderU.UserU.FullName)
                : query.OrderBy(q => q.ProviderU.UserU.FullName),
            "amount" => sortDir == "desc"
                ? query.OrderByDescending(q => q.QuoteAmount)
                : query.OrderBy(q => q.QuoteAmount),
            _ => sortDir == "desc"
                ? query.OrderByDescending(q => q.QuoteDate)
                : query.OrderBy(q => q.QuoteDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new ProviderQuoteItemVm
            {
                Uid = q.Uid,
                RequestLabel = $"#{q.RequestUid} - {q.RequestU.CustomerU.FullName}",
                ProviderName = q.ProviderU.UserU.FullName,
                QuoteAmount = q.QuoteAmount,
                EstimatedArrivalMinutes = q.EstimatedArrivalMinutes,
                DistanceKm = q.DistanceKm,
                QuoteDate = q.QuoteDate
            })
            .ToListAsync(cancellationToken);

        return new ProviderQuoteListVm
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

    public async Task<ProviderQuoteDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _quoteRepo.Query()
            .Where(q => q.Uid == id)
            .Select(q => new ProviderQuoteDetailsVm
            {
                Uid = q.Uid,
                RequestUid = q.RequestUid,
                RequestLabel = $"#{q.RequestUid} - {q.RequestU.CustomerU.FullName} ({q.RequestU.CategoryU.CategoryName})",
                ProviderUid = q.ProviderUid,
                ProviderName = q.ProviderU.UserU.FullName,
                QuoteAmount = q.QuoteAmount,
                EstimatedArrivalMinutes = q.EstimatedArrivalMinutes,
                DistanceKm = q.DistanceKm,
                Remarks = q.Remarks,
                QuoteDate = q.QuoteDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProviderQuoteFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _quoteRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new ProviderQuoteFormVm
        {
            Uid = entity.Uid,
            RequestUid = entity.RequestUid,
            ProviderUid = entity.ProviderUid,
            QuoteAmount = entity.QuoteAmount,
            EstimatedArrivalMinutes = entity.EstimatedArrivalMinutes,
            DistanceKm = entity.DistanceKm,
            Remarks = entity.Remarks
        }, cancellationToken);
    }

    public async Task<ProviderQuoteDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _quoteRepo.Query()
            .Where(q => q.Uid == id)
            .Select(q => new ProviderQuoteDeleteVm
            {
                Uid = q.Uid,
                RequestLabel = $"#{q.RequestUid} - {q.RequestU.CustomerU.FullName}",
                ProviderName = q.ProviderU.UserU.FullName,
                QuoteAmount = q.QuoteAmount,
                QuoteDate = q.QuoteDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ProviderQuoteFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateForeignKeysAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        var entity = new ProviderQuote
        {
            RequestUid = model.RequestUid,
            ProviderUid = model.ProviderUid,
            QuoteAmount = model.QuoteAmount,
            EstimatedArrivalMinutes = model.EstimatedArrivalMinutes,
            DistanceKm = model.DistanceKm,
            Remarks = model.Remarks?.Trim(),
            QuoteDate = DateTime.Now
        };

        await _quoteRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ProviderQuoteFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _quoteRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null) return (false, "Provider quote not found.");

        var validationError = await ValidateForeignKeysAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        entity.RequestUid = model.RequestUid;
        entity.ProviderUid = model.ProviderUid;
        entity.QuoteAmount = model.QuoteAmount;
        entity.EstimatedArrivalMinutes = model.EstimatedArrivalMinutes;
        entity.DistanceKm = model.DistanceKm;
        entity.Remarks = model.Remarks?.Trim();

        await _quoteRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _quoteRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return (false, "Provider quote not found.");

        await _quoteRepo.DeleteAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<ProviderQuoteFormVm> PopulateFormAsync(
        ProviderQuoteFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Requests = await GetRequestOptionsAsync(cancellationToken);
        model.Providers = await GetProviderOptionsAsync(cancellationToken);
        return model;
    }

    private async Task<string?> ValidateForeignKeysAsync(
        ProviderQuoteFormVm model,
        CancellationToken cancellationToken)
    {
        var requestExists = await _requestRepo.Query()
            .AnyAsync(r => r.Uid == model.RequestUid, cancellationToken);
        if (!requestExists) return "Selected service request does not exist.";

        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists) return "Selected provider does not exist.";

        return null;
    }
}

using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class CommissionRuleService : ICommissionRuleService
{
    private static readonly string[] ValidScopes = ["Global", "Category", "Provider"];
    private static readonly string[] ValidRuleTypes = ["Percentage", "Fixed"];

    private readonly AppDbContext _db;

    public CommissionRuleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommissionRuleListVm> GetListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;

        var query = _db.CommissionRules.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.Scope.Contains(term) ||
                r.RuleType.Contains(term) ||
                (r.Category != null && r.Category.CategoryName.Contains(term)) ||
                (r.Provider != null && r.Provider.FullName.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CommissionRuleItemVm
            {
                Uid = r.Uid,
                Scope = r.Scope,
                CategoryName = r.Category != null ? r.Category.CategoryName : null,
                ProviderName = r.Provider != null ? r.Provider.FullName : null,
                RuleType = r.RuleType,
                Value = r.Value,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsActive = r.IsActive
            })
            .ToListAsync(cancellationToken);

        return new CommissionRuleListVm
        {
            Items = items,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CommissionRuleDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.CommissionRules
            .AsNoTracking()
            .Where(r => r.Uid == id)
            .Select(r => new CommissionRuleDetailsVm
            {
                Uid = r.Uid,
                Scope = r.Scope,
                CategoryName = r.Category != null ? r.Category.CategoryName : null,
                ProviderName = r.Provider != null ? r.Provider.FullName : null,
                RuleType = r.RuleType,
                Value = r.Value,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsActive = r.IsActive,
                CreatedOn = r.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CommissionRuleFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CommissionRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Uid == id, cancellationToken);

        if (entity == null) return null;

        return await PopulateFormAsync(new CommissionRuleFormVm
        {
            Uid = entity.Uid,
            Scope = entity.Scope,
            CategoryUid = entity.CategoryUid,
            ProviderUid = entity.ProviderUid,
            RuleType = entity.RuleType,
            Value = entity.Value,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo,
            IsActive = entity.IsActive
        }, cancellationToken);
    }

    public async Task<CommissionRuleDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.CommissionRules
            .AsNoTracking()
            .Where(r => r.Uid == id)
            .Select(r => new CommissionRuleDeleteVm
            {
                Uid = r.Uid,
                Scope = r.Scope,
                RuleType = r.RuleType,
                Value = r.Value,
                TargetLabel = r.Scope == "Category" && r.Category != null
                    ? r.Category.CategoryName
                    : r.Scope == "Provider" && r.Provider != null
                        ? r.Provider.FullName
                        : "Global"
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CommissionRuleFormVm> PopulateFormAsync(
        CommissionRuleFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.ScopeOptions = ValidScopes
            .Select(s => new SelectListItem { Value = s, Text = s })
            .ToList();
        model.RuleTypeOptions = ValidRuleTypes
            .Select(t => new SelectListItem { Value = t, Text = t })
            .ToList();
        model.Categories = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem { Value = c.Uid.ToString(), Text = c.CategoryName })
            .ToListAsync(cancellationToken);
        model.Providers = await _db.Providers
            .AsNoTracking()
            .Where(p => p.User.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem { Value = p.Uid.ToString(), Text = p.FullName })
            .ToListAsync(cancellationToken);
        return model;
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        CommissionRuleFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        ApplyScopeNulls(model);

        _db.CommissionRules.Add(new CommissionRule
        {
            Scope = model.Scope.Trim(),
            CategoryUid = model.CategoryUid,
            ProviderUid = model.ProviderUid,
            RuleType = model.RuleType.Trim(),
            Value = model.Value,
            EffectiveFrom = model.EffectiveFrom.Date,
            EffectiveTo = model.EffectiveTo?.Date,
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        CommissionRuleFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.CommissionRules
            .FirstOrDefaultAsync(r => r.Uid == model.Uid, cancellationToken);

        if (entity == null) return (false, "Commission rule not found.");

        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        ApplyScopeNulls(model);

        entity.Scope = model.Scope.Trim();
        entity.CategoryUid = model.CategoryUid;
        entity.ProviderUid = model.ProviderUid;
        entity.RuleType = model.RuleType.Trim();
        entity.Value = model.Value;
        entity.EffectiveFrom = model.EffectiveFrom.Date;
        entity.EffectiveTo = model.EffectiveTo?.Date;
        entity.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CommissionRules
            .FirstOrDefaultAsync(r => r.Uid == id, cancellationToken);

        if (entity == null) return (false, "Commission rule not found.");

        _db.CommissionRules.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private async Task<string?> ValidateAsync(CommissionRuleFormVm model, CancellationToken cancellationToken)
    {
        if (!ValidScopes.Contains(model.Scope)) return "Invalid scope.";
        if (!ValidRuleTypes.Contains(model.RuleType)) return "Invalid rule type.";

        if (model.RuleType.Equals("Percentage", StringComparison.OrdinalIgnoreCase) && model.Value > 100)
        {
            return "Percentage value cannot exceed 100.";
        }

        if (model.EffectiveTo.HasValue && model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
        {
            return "Effective To cannot be earlier than Effective From.";
        }

        if (model.Scope.Equals("Category", StringComparison.OrdinalIgnoreCase))
        {
            if (!model.CategoryUid.HasValue || model.CategoryUid.Value <= 0)
                return "Category is required for Category scope.";

            var exists = await _db.ServiceCategories
                .AnyAsync(c => c.Uid == model.CategoryUid.Value, cancellationToken);
            if (!exists) return "Selected category was not found.";
        }

        if (model.Scope.Equals("Provider", StringComparison.OrdinalIgnoreCase))
        {
            if (!model.ProviderUid.HasValue || model.ProviderUid.Value <= 0)
                return "Provider is required for Provider scope.";

            var exists = await _db.Providers
                .AnyAsync(p => p.Uid == model.ProviderUid.Value, cancellationToken);
            if (!exists) return "Selected provider was not found.";
        }

        return null;
    }

    private static void ApplyScopeNulls(CommissionRuleFormVm model)
    {
        if (model.Scope.Equals("Global", StringComparison.OrdinalIgnoreCase))
        {
            model.CategoryUid = null;
            model.ProviderUid = null;
        }
        else if (model.Scope.Equals("Category", StringComparison.OrdinalIgnoreCase))
        {
            model.ProviderUid = null;
        }
        else if (model.Scope.Equals("Provider", StringComparison.OrdinalIgnoreCase))
        {
            model.CategoryUid = null;
        }
    }
}

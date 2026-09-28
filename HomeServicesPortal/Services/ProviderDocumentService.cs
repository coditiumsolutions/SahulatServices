using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

/// <summary>
/// Admin MVC service for ProviderDocuments (live schema: profile + CNIC paths).
/// Joins Providers ↔ ProviderDocuments on MobileNo (1:1); ProviderUID remains the UID FK.
/// </summary>
public class ProviderDocumentService : IProviderDocumentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ProviderDocumentService> _logger;

    public ProviderDocumentService(
        AppDbContext db,
        IFileStorageService fileStorage,
        ILogger<ProviderDocumentService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(
        int? includeProviderUid = null,
        CancellationToken cancellationToken = default)
    {
        var providersWithDocs = _db.ProviderDocuments.AsNoTracking().Select(d => d.MobileNo);

        return await _db.Providers
            .AsNoTracking()
            .Where(p => includeProviderUid.HasValue && p.Uid == includeProviderUid.Value
                        || !providersWithDocs.Contains(p.MobileNo))
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.FullName + " (" + p.MobileNo + ")"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderDocumentListVm> GetListAsync(
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

        var query =
            from d in _db.ProviderDocuments.AsNoTracking()
            join p in _db.Providers.AsNoTracking() on d.MobileNo equals p.MobileNo
            select new { Document = d, Provider = p };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Provider.FullName.Contains(term)
                || x.Provider.Cnic.Contains(term)
                || x.Document.MobileNo.Contains(term)
                || x.Document.ProviderUid.ToString() == term);
        }

        query = sort switch
        {
            "verified" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Provider.IsVerified).ThenBy(x => x.Provider.FullName)
                : query.OrderBy(x => x.Provider.IsVerified).ThenBy(x => x.Provider.FullName),
            "created" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Document.CreatedOn)
                : query.OrderBy(x => x.Document.CreatedOn),
            "mobile" => sortDir == "desc"
                ? query.OrderByDescending(x => x.Document.MobileNo)
                : query.OrderBy(x => x.Document.MobileNo),
            _ => sortDir == "desc"
                ? query.OrderByDescending(x => x.Provider.FullName)
                : query.OrderBy(x => x.Provider.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProviderDocumentItemVm
            {
                Uid = x.Document.Uid,
                ProviderUid = x.Document.ProviderUid,
                MobileNo = x.Document.MobileNo,
                ProviderName = x.Provider.FullName,
                ProfilePhotoPath = x.Document.ProfilePhotoPath,
                CnicFrontImagePath = x.Document.CnicFrontImagePath,
                CnicBackImagePath = x.Document.CnicBackImagePath,
                PoliceVerificationPath = x.Document.PoliceVerificationPath,
                IsVerified = x.Provider.IsVerified,
                CreatedOn = x.Document.CreatedOn,
                UpdatedOn = x.Document.UpdatedOn
            })
            .ToListAsync(cancellationToken);

        return new ProviderDocumentListVm
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

    public async Task<ProviderDocumentDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await (
            from d in _db.ProviderDocuments.AsNoTracking()
            join p in _db.Providers.AsNoTracking() on d.MobileNo equals p.MobileNo
            where d.Uid == id
            select new ProviderDocumentDetailsVm
            {
                Uid = d.Uid,
                ProviderUid = d.ProviderUid,
                MobileNo = d.MobileNo,
                ProviderName = p.FullName,
                ProfilePhotoPath = d.ProfilePhotoPath,
                CnicFrontImagePath = d.CnicFrontImagePath,
                CnicBackImagePath = d.CnicBackImagePath,
                PoliceVerificationPath = d.PoliceVerificationPath,
                IsVerified = p.IsVerified,
                VerifiedOn = d.VerifiedOn,
                VerifiedBy = d.VerifiedBy,
                VerificationRemarks = d.VerificationRemarks,
                CreatedOn = d.CreatedOn,
                UpdatedOn = d.UpdatedOn
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProviderDocumentFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ProviderDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Uid == id, cancellationToken);
        if (entity == null) return null;

        var providerVerified = await _db.Providers.AsNoTracking()
            .Where(p => p.Uid == entity.ProviderUid)
            .Select(p => p.IsVerified)
            .FirstOrDefaultAsync(cancellationToken);

        return await PopulateFormAsync(new ProviderDocumentFormVm
        {
            Uid = entity.Uid,
            ProviderUid = entity.ProviderUid,
            MobileNo = entity.MobileNo,
            ExistingProfilePhotoPath = entity.ProfilePhotoPath,
            ExistingCnicFrontPath = entity.CnicFrontImagePath,
            ExistingCnicBackPath = entity.CnicBackImagePath,
            ExistingPoliceVerificationPath = entity.PoliceVerificationPath,
            IsVerified = providerVerified,
            VerificationRemarks = entity.VerificationRemarks
        }, cancellationToken);
    }

    public async Task<ProviderDocumentDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await (
            from d in _db.ProviderDocuments.AsNoTracking()
            join p in _db.Providers.AsNoTracking() on d.MobileNo equals p.MobileNo
            where d.Uid == id
            select new ProviderDocumentDeleteVm
            {
                Uid = d.Uid,
                ProviderUid = d.ProviderUid,
                MobileNo = d.MobileNo,
                ProviderName = p.FullName,
                ProfilePhotoPath = d.ProfilePhotoPath,
                CnicFrontImagePath = d.CnicFrontImagePath,
                CnicBackImagePath = d.CnicBackImagePath,
                PoliceVerificationPath = d.PoliceVerificationPath,
                IsVerified = p.IsVerified
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ProviderDocumentFormVm model,
        CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (provider == null)
        {
            return (false, "Selected provider does not exist.");
        }

        if (await _db.ProviderDocuments.AnyAsync(
                d => d.ProviderUid == model.ProviderUid || d.MobileNo == provider.MobileNo,
                cancellationToken))
        {
            return (false, "This provider already has a documents record. Edit the existing one instead.");
        }

        if (model.ProfilePhoto == null || model.ProfilePhoto.Length == 0
            || model.CnicFront == null || model.CnicFront.Length == 0
            || model.CnicBack == null || model.CnicBack.Length == 0)
        {
            return (false, "Profile photo, CNIC front, and CNIC back images are all required.");
        }

        var profile = await _fileStorage.SaveProviderImageAsync(
            model.ProviderUid, model.ProfilePhoto, "profile.jpg", cancellationToken);
        if (!profile.Success) return (false, profile.Error);

        var front = await _fileStorage.SaveProviderImageAsync(
            model.ProviderUid, model.CnicFront, "cnic_front.jpg", cancellationToken);
        if (!front.Success) return (false, front.Error);

        var back = await _fileStorage.SaveProviderImageAsync(
            model.ProviderUid, model.CnicBack, "cnic_back.jpg", cancellationToken);
        if (!back.Success) return (false, back.Error);

        // Added v3.20 — optional, unlike the three images above.
        string? policeVerificationPath = null;
        if (model.PoliceVerification is { Length: > 0 })
        {
            var police = await _fileStorage.SaveProviderImageAsync(
                model.ProviderUid, model.PoliceVerification, "police_verification.jpg", cancellationToken);
            if (!police.Success) return (false, police.Error);
            policeVerificationPath = police.RelativePath;
        }

        var entity = new ProviderDocument
        {
            ProviderUid = model.ProviderUid,
            MobileNo = provider.MobileNo,
            ProfilePhotoPath = profile.RelativePath,
            CnicFrontImagePath = front.RelativePath,
            CnicBackImagePath = back.RelativePath,
            PoliceVerificationPath = policeVerificationPath,
            IsVerified = false,
            CreatedOn = DateTime.Now
        };

        _db.ProviderDocuments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Admin created ProviderDocuments for provider {ProviderUid} / {MobileNo}",
            model.ProviderUid,
            provider.MobileNo);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ProviderDocumentFormVm model,
        int? verifiedByUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ProviderDocuments
            .FirstOrDefaultAsync(d => d.Uid == model.Uid, cancellationToken);
        if (entity == null) return (false, "Document record not found.");

        if (entity.ProviderUid != model.ProviderUid)
        {
            return (false, "Provider cannot be changed for an existing document record.");
        }

        if (model.ProfilePhoto is { Length: > 0 })
        {
            var result = await _fileStorage.SaveProviderImageAsync(
                entity.ProviderUid, model.ProfilePhoto, "profile.jpg", cancellationToken);
            if (!result.Success) return (false, result.Error);
            entity.ProfilePhotoPath = result.RelativePath;
        }

        if (model.CnicFront is { Length: > 0 })
        {
            var result = await _fileStorage.SaveProviderImageAsync(
                entity.ProviderUid, model.CnicFront, "cnic_front.jpg", cancellationToken);
            if (!result.Success) return (false, result.Error);
            entity.CnicFrontImagePath = result.RelativePath;
        }

        if (model.CnicBack is { Length: > 0 })
        {
            var result = await _fileStorage.SaveProviderImageAsync(
                entity.ProviderUid, model.CnicBack, "cnic_back.jpg", cancellationToken);
            if (!result.Success) return (false, result.Error);
            entity.CnicBackImagePath = result.RelativePath;
        }

        if (model.PoliceVerification is { Length: > 0 })
        {
            var result = await _fileStorage.SaveProviderImageAsync(
                entity.ProviderUid, model.PoliceVerification, "police_verification.jpg", cancellationToken);
            if (!result.Success) return (false, result.Error);
            entity.PoliceVerificationPath = result.RelativePath;
        }

        if (string.IsNullOrWhiteSpace(entity.ProfilePhotoPath)
            || string.IsNullOrWhiteSpace(entity.CnicFrontImagePath)
            || string.IsNullOrWhiteSpace(entity.CnicBackImagePath))
        {
            return (false, "All three images (profile, CNIC front, CNIC back) must be present.");
        }

        // Verification is managed only from the Service Providers page (IsVerified toggle).
        entity.VerificationRemarks = string.IsNullOrWhiteSpace(model.VerificationRemarks)
            ? null
            : model.VerificationRemarks.Trim();

        entity.UpdatedOn = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin updated ProviderDocuments UID {Uid}", entity.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> VerifyAsync(
        int id,
        int? verifiedByUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ProviderDocuments.FirstOrDefaultAsync(d => d.Uid == id, cancellationToken);
        if (entity == null) return (false, "Document record not found.");

        var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Uid == entity.ProviderUid, cancellationToken);
        if (provider == null) return (false, "Provider not found.");

        provider.IsVerified = true;
        entity.VerifiedOn = DateTime.Now;
        entity.VerifiedBy = verifiedByUserId;
        entity.UpdatedOn = DateTime.Now;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin verified provider {ProviderUid} via documents UID {Uid}", entity.ProviderUid, entity.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ProviderDocuments.FirstOrDefaultAsync(d => d.Uid == id, cancellationToken);
        if (entity == null) return (false, "Document record not found.");

        var providerUid = entity.ProviderUid;
        _db.ProviderDocuments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _fileStorage.DeleteProviderDocumentFiles(providerUid);
        _logger.LogInformation("Admin deleted ProviderDocuments for provider {ProviderUid}", providerUid);
        return (true, null);
    }

    public async Task<ProviderDocumentFormVm> PopulateFormAsync(
        ProviderDocumentFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Providers = await GetProviderOptionsAsync(
            model.Uid > 0 ? model.ProviderUid : null,
            cancellationToken);
        return model;
    }
}

using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using ServiceProviderEntity = HomeServicesPortal.Models.Entities.ServiceProvider;

namespace HomeServicesPortal.Services;

public class ProviderDocumentService : IProviderDocumentService
{
    private static readonly string[] ValidDocumentTypes =
        ["CNIC", "License", "Certificate", "Insurance", "Profile Picture", "Other"];

    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp"];

    private readonly IRepository<ProviderDocument> _documentRepo;
    private readonly IRepository<ServiceProviderEntity> _providerRepo;
    private readonly IWebHostEnvironment _env;

    public ProviderDocumentService(
        IRepository<ProviderDocument> documentRepo,
        IRepository<ServiceProviderEntity> providerRepo,
        IWebHostEnvironment env)
    {
        _documentRepo = documentRepo;
        _providerRepo = providerRepo;
        _env = env;
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

    public List<SelectListItem> GetDocumentTypeOptions()
    {
        return ValidDocumentTypes.Select(t => new SelectListItem { Value = t, Text = t }).ToList();
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

        var query = _documentRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.ProviderU.FullName.Contains(term) ||
                (d.DocumentType != null && d.DocumentType.Contains(term)) ||
                (d.DocumentNo != null && d.DocumentNo.Contains(term)));
        }

        query = sort switch
        {
            "type" => sortDir == "desc"
                ? query.OrderByDescending(d => d.DocumentType)
                : query.OrderBy(d => d.DocumentType),
            "number" => sortDir == "desc"
                ? query.OrderByDescending(d => d.DocumentNo)
                : query.OrderBy(d => d.DocumentNo),
            "expiry" => sortDir == "desc"
                ? query.OrderByDescending(d => d.ExpiryDate)
                : query.OrderBy(d => d.ExpiryDate),
            _ => sortDir == "desc"
                ? query.OrderByDescending(d => d.ProviderU.FullName)
                : query.OrderBy(d => d.ProviderU.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new ProviderDocumentItemVm
            {
                Uid = d.Uid,
                ProviderName = d.ProviderU.FullName,
                DocumentType = d.DocumentType,
                DocumentNo = d.DocumentNo,
                FilePath = d.FilePath,
                ExpiryDate = d.ExpiryDate
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
        return await _documentRepo.Query()
            .Where(d => d.Uid == id)
            .Select(d => new ProviderDocumentDetailsVm
            {
                Uid = d.Uid,
                ProviderUid = d.ProviderUid,
                ProviderName = d.ProviderU.FullName,
                DocumentType = d.DocumentType,
                DocumentNo = d.DocumentNo,
                FilePath = d.FilePath,
                ExpiryDate = d.ExpiryDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProviderDocumentFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new ProviderDocumentFormVm
        {
            Uid = entity.Uid,
            ProviderUid = entity.ProviderUid,
            DocumentType = entity.DocumentType,
            DocumentNo = entity.DocumentNo,
            ExpiryDate = entity.ExpiryDate,
            ExistingFilePath = entity.FilePath
        }, cancellationToken);
    }

    public async Task<ProviderDocumentDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _documentRepo.Query()
            .Where(d => d.Uid == id)
            .Select(d => new ProviderDocumentDeleteVm
            {
                Uid = d.Uid,
                ProviderName = d.ProviderU.FullName,
                DocumentType = d.DocumentType,
                DocumentNo = d.DocumentNo,
                FilePath = d.FilePath,
                ExpiryDate = d.ExpiryDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ProviderDocumentFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, requireFile: true, cancellationToken);
        if (validationError != null) return (false, validationError);

        var (fileSuccess, filePath, fileError) = await SaveFileAsync(model.ProviderUid, model.DocumentFile!, cancellationToken);
        if (!fileSuccess) return (false, fileError);

        var entity = new ProviderDocument
        {
            ProviderUid = model.ProviderUid,
            DocumentType = model.DocumentType?.Trim(),
            DocumentNo = model.DocumentNo?.Trim(),
            FilePath = filePath,
            ExpiryDate = model.ExpiryDate
        };

        await _documentRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ProviderDocumentFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null) return (false, "Document not found.");

        var validationError = await ValidateAsync(model, requireFile: false, cancellationToken);
        if (validationError != null) return (false, validationError);

        if (model.DocumentFile != null && model.DocumentFile.Length > 0)
        {
            DeletePhysicalFile(entity.FilePath);
            var (fileSuccess, filePath, fileError) = await SaveFileAsync(model.ProviderUid, model.DocumentFile, cancellationToken);
            if (!fileSuccess) return (false, fileError);
            entity.FilePath = filePath;
        }
        else if (string.IsNullOrWhiteSpace(entity.FilePath) && string.IsNullOrWhiteSpace(model.ExistingFilePath))
        {
            return (false, "Document file is required.");
        }

        entity.ProviderUid = model.ProviderUid;
        entity.DocumentType = model.DocumentType?.Trim();
        entity.DocumentNo = model.DocumentNo?.Trim();
        entity.ExpiryDate = model.ExpiryDate;

        await _documentRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _documentRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return (false, "Document not found.");

        var filePath = entity.FilePath;
        await _documentRepo.DeleteAsync(entity, cancellationToken);
        DeletePhysicalFile(filePath);
        return (true, null);
    }

    public async Task<ProviderDocumentFormVm> PopulateFormAsync(
        ProviderDocumentFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Providers = await GetProviderOptionsAsync(cancellationToken);
        model.DocumentTypes = GetDocumentTypeOptions();
        return model;
    }

    private async Task<string?> ValidateAsync(
        ProviderDocumentFormVm model,
        bool requireFile,
        CancellationToken cancellationToken)
    {
        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists) return "Selected provider does not exist.";

        if (!string.IsNullOrWhiteSpace(model.DocumentType) &&
            !ValidDocumentTypes.Contains(model.DocumentType))
        {
            return "Invalid document type.";
        }

        if (requireFile && (model.DocumentFile == null || model.DocumentFile.Length == 0))
        {
            return "Document file is required.";
        }

        if (model.DocumentFile != null && model.DocumentFile.Length > 0)
        {
            var ext = Path.GetExtension(model.DocumentFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return "File must be PDF, JPG, PNG, or WEBP.";
            }

            if (model.DocumentFile.Length > 5 * 1024 * 1024)
            {
                return "File must be under 5 MB.";
            }
        }

        return null;
    }

    private async Task<(bool Success, string? FilePath, string? Error)> SaveFileAsync(
        int providerUid,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "documents");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"doc_{providerUid}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var physicalPath = Path.Combine(uploadDir, fileName);
        var relativePath = $"/uploads/documents/{fileName}";

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return (true, relativePath, null);
    }

    private void DeletePhysicalFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        var physicalPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}

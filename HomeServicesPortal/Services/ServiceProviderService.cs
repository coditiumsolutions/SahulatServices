using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using ServiceProviderEntity = HomeServicesPortal.Models.Entities.ServiceProvider;

namespace HomeServicesPortal.Services;

public class ServiceProviderService : IServiceProviderService
{
    private const string ProfileDocType = "ProfilePicture";
    private readonly IRepository<ServiceProviderEntity> _providerRepo;
    private readonly IRepository<ServiceCategory> _categoryRepo;
    private readonly IRepository<ProviderDocument> _documentRepo;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ServiceProviderService> _logger;

    public ServiceProviderService(
        IRepository<ServiceProviderEntity> providerRepo,
        IRepository<ServiceCategory> categoryRepo,
        IRepository<ProviderDocument> documentRepo,
        IWebHostEnvironment env,
        ILogger<ServiceProviderService> logger)
    {
        _providerRepo = providerRepo;
        _categoryRepo = categoryRepo;
        _documentRepo = documentRepo;
        _env = env;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServiceProviderApiDto>> GetActiveProvidersForApiAsync(
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        var profileMap = await GetProfilePictureMapAsync(cancellationToken);
        var categoryMap = await GetActiveCategoryMapAsync(cancellationToken);

        var query = _providerRepo.Query().Where(p => p.IsActive != false);

        if (categoryId.HasValue)
        {
            if (!categoryMap.ContainsKey(categoryId.Value))
            {
                return [];
            }

            query = query.Where(p => p.CategoryUid == categoryId.Value);
        }

        var providers = await query
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);

        return providers
            .Where(p => categoryMap.ContainsKey(p.CategoryUid))
            .Select(p => MapProviderApiDto(p, categoryMap[p.CategoryUid], profileMap.GetValueOrDefault(p.Uid)))
            .ToList();
    }

    public async Task<ServiceProviderApiDto?> GetActiveProviderForApiAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == id && p.IsActive != false, cancellationToken);

        if (provider == null)
        {
            return null;
        }

        var categoryName = await _categoryRepo.Query()
            .Where(c => c.Uid == provider.CategoryUid && c.IsActive != false)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync(cancellationToken);

        if (categoryName == null)
        {
            return null;
        }

        var profilePath = await _documentRepo.Query()
            .Where(d => d.ProviderUid == id && d.DocumentType == ProfileDocType)
            .Select(d => d.FilePath)
            .FirstOrDefaultAsync(cancellationToken);

        return MapProviderApiDto(provider, categoryName, profilePath);
    }

    public async Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _categoryRepo.Query()
            .Where(c => c.IsActive != false)
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.CategoryName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceProviderListVm> GetListAsync(
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

        var categories = await _categoryRepo.Query()
            .Select(c => new { c.Uid, c.CategoryName })
            .ToListAsync(cancellationToken);

        var categoryMap = categories.ToDictionary(c => c.Uid, c => c.CategoryName);

        var profileDocs = await _documentRepo.Query()
            .Where(d => d.DocumentType == ProfileDocType)
            .Select(d => new { d.ProviderUid, d.FilePath })
            .ToListAsync(cancellationToken);

        var profileMap = profileDocs
            .GroupBy(d => d.ProviderUid)
            .ToDictionary(g => g.Key, g => g.First().FilePath);

        var query = _providerRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.FullName.Contains(term) ||
                (p.MobileNo != null && p.MobileNo.Contains(term)) ||
                (p.Cnic != null && p.Cnic.Contains(term)));
        }

        query = sort switch
        {
            "category" => sortDir == "desc"
                ? query.OrderByDescending(p => p.CategoryUid)
                : query.OrderBy(p => p.CategoryUid),
            "rating" => sortDir == "desc"
                ? query.OrderByDescending(p => p.Rating)
                : query.OrderBy(p => p.Rating),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(p => p.CreatedOn)
                : query.OrderBy(p => p.CreatedOn),
            _ => sortDir == "desc"
                ? query.OrderByDescending(p => p.FullName)
                : query.OrderBy(p => p.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var providers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = providers.Select(p => new ServiceProviderItemVm
        {
            Uid = p.Uid,
            FullName = p.FullName,
            MobileNo = p.MobileNo,
            Cnic = p.Cnic,
            CategoryName = categoryMap.GetValueOrDefault(p.CategoryUid, "—"),
            ExperienceYears = p.ExperienceYears,
            Rating = p.Rating,
            IsVerified = p.IsVerified ?? false,
            IsActive = p.IsActive ?? true,
            ProfilePicturePath = profileMap.GetValueOrDefault(p.Uid),
            CreatedOn = p.CreatedOn
        }).ToList();

        return new ServiceProviderListVm
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

    public async Task<ServiceProviderDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == id, cancellationToken);

        if (provider == null) return null;

        var categoryName = await _categoryRepo.Query()
            .Where(c => c.Uid == provider.CategoryUid)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        var profilePath = await _documentRepo.Query()
            .Where(d => d.ProviderUid == id && d.DocumentType == ProfileDocType)
            .Select(d => d.FilePath)
            .FirstOrDefaultAsync(cancellationToken);

        return MapDetails(provider, categoryName, profilePath);
    }

    public async Task<ServiceProviderFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepo.GetByIdAsync(id, cancellationToken);
        if (provider == null) return null;

        var profilePath = await _documentRepo.Query()
            .Where(d => d.ProviderUid == id && d.DocumentType == ProfileDocType)
            .Select(d => d.FilePath)
            .FirstOrDefaultAsync(cancellationToken);

        return new ServiceProviderFormVm
        {
            Uid = provider.Uid,
            FullName = provider.FullName,
            MobileNo = provider.MobileNo,
            Cnic = provider.Cnic,
            CategoryUid = provider.CategoryUid,
            ExperienceYears = provider.ExperienceYears,
            Rating = provider.Rating,
            IsVerified = provider.IsVerified ?? false,
            IsActive = provider.IsActive ?? true,
            ExistingProfilePicturePath = profilePath,
            Categories = await GetCategoryOptionsAsync(cancellationToken)
        };
    }

    public async Task<ServiceProviderDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == id, cancellationToken);

        if (provider == null) return null;

        var categoryName = await _categoryRepo.Query()
            .Where(c => c.Uid == provider.CategoryUid)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        return new ServiceProviderDeleteVm
        {
            Uid = provider.Uid,
            FullName = provider.FullName,
            MobileNo = provider.MobileNo,
            CategoryName = categoryName
        };
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _categoryRepo.Query()
            .AnyAsync(c => c.Uid == model.CategoryUid, cancellationToken);

        if (!categoryExists)
        {
            return (false, "Selected category does not exist.");
        }

        var entity = new ServiceProviderEntity
        {
            FullName = model.FullName.Trim(),
            MobileNo = model.MobileNo?.Trim(),
            Cnic = model.Cnic?.Trim(),
            CategoryUid = model.CategoryUid,
            ExperienceYears = model.ExperienceYears ?? 0,
            Rating = model.Rating ?? 0,
            IsVerified = model.IsVerified,
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now
        };

        await _providerRepo.AddAsync(entity, cancellationToken);

        if (entity.Uid <= 0)
        {
            return (false, "Failed to generate provider ID after save.");
        }

        if (model.ProfilePicture != null)
        {
            var (ok, err) = await SaveProfilePictureAsync(entity.Uid, model.ProfilePicture, cancellationToken);
            if (!ok) return (false, err);
        }

        _logger.LogInformation("S-Provider {Name} created with UID {Uid}.", entity.FullName, entity.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _providerRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Provider not found.");
        }

        var categoryExists = await _categoryRepo.Query()
            .AnyAsync(c => c.Uid == model.CategoryUid, cancellationToken);

        if (!categoryExists)
        {
            return (false, "Selected category does not exist.");
        }

        entity.FullName = model.FullName.Trim();
        entity.MobileNo = model.MobileNo?.Trim();
        entity.Cnic = model.Cnic?.Trim();
        entity.CategoryUid = model.CategoryUid;
        entity.ExperienceYears = model.ExperienceYears ?? 0;
        entity.Rating = model.Rating ?? 0;
        entity.IsVerified = model.IsVerified;
        entity.IsActive = model.IsActive;

        await _providerRepo.UpdateAsync(entity, cancellationToken);

        if (model.ProfilePicture != null)
        {
            var (ok, err) = await SaveProfilePictureAsync(entity.Uid, model.ProfilePicture, cancellationToken);
            if (!ok) return (false, err);
        }

        _logger.LogInformation("S-Provider {Uid} updated.", model.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _providerRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (false, "Provider not found.");
        }

        try
        {
            await _providerRepo.DeleteAsync(entity, cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this provider because it is linked to bookings, quotes, or other records.");
        }
    }

    private static ServiceProviderDetailsVm MapDetails(
        ServiceProviderEntity provider,
        string categoryName,
        string? profilePath)
    {
        return new ServiceProviderDetailsVm
        {
            Uid = provider.Uid,
            FullName = provider.FullName,
            MobileNo = provider.MobileNo,
            Cnic = provider.Cnic,
            CategoryName = categoryName,
            ExperienceYears = provider.ExperienceYears,
            Rating = provider.Rating,
            IsVerified = provider.IsVerified ?? false,
            IsActive = provider.IsActive ?? true,
            ProfilePicturePath = profilePath,
            CreatedOn = provider.CreatedOn
        };
    }

    private async Task<(bool Success, string? Error)> SaveProfilePictureAsync(
        int providerUid,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            return (false, "Profile picture must be JPG, PNG, or WEBP.");
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return (false, "Profile picture must be under 2 MB.");
        }

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "providers");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"provider_{providerUid}{ext}";
        var physicalPath = Path.Combine(uploadDir, fileName);
        var relativePath = $"/uploads/providers/{fileName}";

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var existingUid = await _documentRepo.Query()
            .Where(d => d.ProviderUid == providerUid && d.DocumentType == ProfileDocType)
            .Select(d => d.Uid)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUid == 0)
        {
            await _documentRepo.AddAsync(new ProviderDocument
            {
                ProviderUid = providerUid,
                DocumentType = ProfileDocType,
                DocumentNo = "PROFILE",
                FilePath = relativePath
            }, cancellationToken);
        }
        else
        {
            var doc = await _documentRepo.GetByIdAsync(existingUid, cancellationToken);
            if (doc != null)
            {
                doc.FilePath = relativePath;
                await _documentRepo.UpdateAsync(doc, cancellationToken);
            }
        }

        return (true, null);
    }

    private async Task<Dictionary<int, string>> GetActiveCategoryMapAsync(CancellationToken cancellationToken)
    {
        return await _categoryRepo.Query()
            .Where(c => c.IsActive != false)
            .ToDictionaryAsync(c => c.Uid, c => c.CategoryName, cancellationToken);
    }

    private async Task<Dictionary<int, string?>> GetProfilePictureMapAsync(CancellationToken cancellationToken)
    {
        var profileDocs = await _documentRepo.Query()
            .Where(d => d.DocumentType == ProfileDocType)
            .Select(d => new { d.ProviderUid, d.FilePath })
            .ToListAsync(cancellationToken);

        return profileDocs
            .GroupBy(d => d.ProviderUid)
            .ToDictionary(g => g.Key, g => g.First().FilePath);
    }

    private static ServiceProviderApiDto MapProviderApiDto(
        ServiceProviderEntity provider,
        string categoryName,
        string? profilePath)
    {
        return new ServiceProviderApiDto
        {
            Id = provider.Uid,
            FullName = provider.FullName,
            MobileNo = provider.MobileNo,
            CategoryId = provider.CategoryUid,
            CategoryName = categoryName,
            ExperienceYears = provider.ExperienceYears ?? 0,
            Rating = provider.Rating ?? 0,
            IsVerified = provider.IsVerified ?? false,
            ProfileImageUrl = profilePath,
            CreatedOn = provider.CreatedOn
        };
    }
}

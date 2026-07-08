using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using HomeServicesPortal.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceProviderService : IServiceProviderService
{
    private const string ProfileDocType = "ProfilePicture";
    private readonly AppDbContext _appDb;
    private readonly IRepository<ProviderProfile> _providerRepo;
    private readonly IRepository<AppUser> _userRepo;
    private readonly IRepository<ServiceCategory> _categoryRepo;
    private readonly IRepository<ServiceRequest> _requestRepo;
    private readonly IRepository<ProviderDocument> _documentRepo;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ServiceProviderService> _logger;

    public ServiceProviderService(
        AppDbContext appDb,
        IRepository<ProviderProfile> providerRepo,
        IRepository<AppUser> userRepo,
        IRepository<ServiceCategory> categoryRepo,
        IRepository<ServiceRequest> requestRepo,
        IRepository<ProviderDocument> documentRepo,
        IWebHostEnvironment env,
        ILogger<ServiceProviderService> logger)
    {
        _appDb = appDb;
        _providerRepo = providerRepo;
        _userRepo = userRepo;
        _categoryRepo = categoryRepo;
        _requestRepo = requestRepo;
        _documentRepo = documentRepo;
        _env = env;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderProfileApiDto>> GetProviderProfilesForApiAsync(
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = ActiveProvidersFromAppDbQuery();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryUid == categoryId.Value);
        }

        return await query
            .OrderBy(p => p.FullName)
            .Select(p => new ProviderProfileApiDto
            {
                Uid = p.Uid,
                UserUid = p.UserUid,
                FullName = p.FullName,
                CategoryUid = p.CategoryUid,
                Cnic = p.Cnic,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderProfileApiDto?> GetProviderProfileByUserUidAsync(
        int userUid,
        CancellationToken cancellationToken = default)
    {
        return await ActiveProvidersFromAppDbQuery()
            .Where(p => p.UserUid == userUid)
            .Select(p => new ProviderProfileApiDto
            {
                Uid = p.Uid,
                UserUid = p.UserUid,
                FullName = p.FullName,
                CategoryUid = p.CategoryUid,
                Cnic = p.Cnic,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProviderServiceRequestResponse> GetServiceRequestsForProviderAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        var provider = await ActiveProvidersQuery()
            .Where(p => p.Uid == providerUid)
            .Select(p => new { p.Uid, p.CategoryUid })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null)
        {
            return new ProviderServiceRequestResponse { Result = ProviderServiceRequestResult.ProviderNotFound };
        }

        if (!provider.CategoryUid.HasValue || provider.CategoryUid <= 0)
        {
            return new ProviderServiceRequestResponse { Result = ProviderServiceRequestResult.CategoryNotAssigned };
        }

        var requests = await _requestRepo.Query()
            .Where(r => r.CategoryUid == provider.CategoryUid)
            .OrderByDescending(r => r.RequestDate)
            .ThenByDescending(r => r.Uid)
            .Select(r => new ProviderServiceRequestApiDto
            {
                Id = r.Uid,
                CategoryId = r.CategoryUid,
                CategoryName = r.CategoryU.CategoryName,
                CustomerId = r.CustomerUid,
                CustomerName = r.CustomerU.FullName,
                CustomerMobile = r.CustomerU.MobileNo,
                RequestTitle = r.CategoryU.CategoryName + " Request #" + r.Uid,
                Description = r.ProblemDescription,
                ServiceAddress = r.ServiceAddress,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                RequestDate = r.RequestDate,
                Status = r.Status
            })
            .ToListAsync(cancellationToken);

        return new ProviderServiceRequestResponse
        {
            Result = ProviderServiceRequestResult.Success,
            Items = requests
        };
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

        var query = _providerRepo.Query()
            .Where(p => p.UserU.UserType == UserTypeConstants.Provider);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                (p.UserU.FullName != null && p.UserU.FullName.Contains(term)) ||
                (p.UserU.MobileNo != null && p.UserU.MobileNo.Contains(term)) ||
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
                ? query.OrderByDescending(p => p.UserU.CreatedOn)
                : query.OrderBy(p => p.UserU.CreatedOn),
            _ => sortDir == "desc"
                ? query.OrderByDescending(p => p.UserU.FullName)
                : query.OrderBy(p => p.UserU.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var providers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = providers.Select(p => new ServiceProviderItemVm
        {
            Uid = p.Uid,
            FullName = p.UserU.FullName ?? "—",
            MobileNo = p.UserU.MobileNo,
            Cnic = p.Cnic,
            CategoryName = p.CategoryUid.HasValue
                ? categoryMap.GetValueOrDefault(p.CategoryUid.Value, "—")
                : "—",
            ExperienceYears = p.ExperienceYears,
            Rating = p.Rating,
            IsVerified = p.IsVerified ?? false,
            IsActive = p.UserU.IsActive ?? true,
            ProfilePicturePath = profileMap.GetValueOrDefault(p.Uid),
            CreatedOn = p.UserU.CreatedOn
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

        var categoryName = provider.CategoryUid.HasValue
            ? await _categoryRepo.Query()
                .Where(c => c.Uid == provider.CategoryUid)
                .Select(c => c.CategoryName)
                .FirstOrDefaultAsync(cancellationToken) ?? "—"
            : "—";

        var profilePath = await _documentRepo.Query()
            .Where(d => d.ProviderUid == id && d.DocumentType == ProfileDocType)
            .Select(d => d.FilePath)
            .FirstOrDefaultAsync(cancellationToken);

        return MapDetails(provider, categoryName, profilePath);
    }

    public async Task<ServiceProviderFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == id, cancellationToken);

        if (provider == null) return null;

        var profilePath = await _documentRepo.Query()
            .Where(d => d.ProviderUid == id && d.DocumentType == ProfileDocType)
            .Select(d => d.FilePath)
            .FirstOrDefaultAsync(cancellationToken);

        return new ServiceProviderFormVm
        {
            Uid = provider.Uid,
            FullName = provider.UserU.FullName ?? string.Empty,
            MobileNo = provider.UserU.MobileNo,
            Cnic = provider.Cnic,
            CategoryUid = provider.CategoryUid ?? 0,
            ExperienceYears = provider.ExperienceYears,
            Rating = provider.Rating,
            IsVerified = provider.IsVerified ?? false,
            IsActive = provider.UserU.IsActive ?? true,
            ExistingProfilePicturePath = profilePath,
            Categories = await GetCategoryOptionsAsync(cancellationToken)
        };
    }

    public async Task<ServiceProviderDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == id, cancellationToken);

        if (provider == null) return null;

        var categoryName = provider.CategoryUid.HasValue
            ? await _categoryRepo.Query()
                .Where(c => c.Uid == provider.CategoryUid)
                .Select(c => c.CategoryName)
                .FirstOrDefaultAsync(cancellationToken) ?? "—"
            : "—";

        return new ServiceProviderDeleteVm
        {
            Uid = provider.Uid,
            FullName = provider.UserU.FullName ?? "—",
            MobileNo = provider.UserU.MobileNo,
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

        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            MobileNo = model.MobileNo?.Trim(),
            Email = BuildAdminProviderEmail(model.MobileNo),
            UserType = UserTypeConstants.Provider,
            IsActive = model.IsActive,
            CreatedOn = DateTime.Now,
            PasswordHash = Guid.NewGuid().ToString("N")
        };

        await _userRepo.AddAsync(user, cancellationToken);

        var profile = new ProviderProfile
        {
            UserUid = user.Uid,
            CategoryUid = model.CategoryUid,
            Cnic = model.Cnic?.Trim(),
            ExperienceYears = model.ExperienceYears ?? 0,
            Rating = model.Rating ?? 0,
            IsVerified = model.IsVerified
        };

        await _providerRepo.AddAsync(profile, cancellationToken);

        if (profile.Uid <= 0)
        {
            return (false, "Failed to generate provider profile ID after save.");
        }

        if (model.ProfilePicture != null)
        {
            var (ok, err) = await SaveProfilePictureAsync(profile.Uid, model.ProfilePicture, cancellationToken);
            if (!ok) return (false, err);
        }

        _logger.LogInformation("Provider profile {Name} created with UID {Uid}.", user.FullName, profile.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken = default)
    {
        var profile = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == model.Uid, cancellationToken);

        if (profile == null)
        {
            return (false, "Provider not found.");
        }

        var categoryExists = await _categoryRepo.Query()
            .AnyAsync(c => c.Uid == model.CategoryUid, cancellationToken);

        if (!categoryExists)
        {
            return (false, "Selected category does not exist.");
        }

        profile.UserU.FullName = model.FullName.Trim();
        profile.UserU.MobileNo = model.MobileNo?.Trim();
        profile.Cnic = model.Cnic?.Trim();
        profile.CategoryUid = model.CategoryUid;
        profile.ExperienceYears = model.ExperienceYears ?? 0;
        profile.Rating = model.Rating ?? 0;
        profile.IsVerified = model.IsVerified;
        profile.UserU.IsActive = model.IsActive;

        await _userRepo.UpdateAsync(profile.UserU, cancellationToken);
        await _providerRepo.UpdateAsync(profile, cancellationToken);

        if (model.ProfilePicture != null)
        {
            var (ok, err) = await SaveProfilePictureAsync(profile.Uid, model.ProfilePicture, cancellationToken);
            if (!ok) return (false, err);
        }

        _logger.LogInformation("Provider profile {Uid} updated.", model.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var profile = await _providerRepo.Query()
            .FirstOrDefaultAsync(p => p.Uid == id, cancellationToken);

        if (profile == null)
        {
            return (false, "Provider not found.");
        }

        var user = profile.UserU;

        try
        {
            await _providerRepo.DeleteAsync(profile, cancellationToken);
            await _userRepo.DeleteAsync(user, cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this provider because it is linked to bookings, quotes, or other records.");
        }
    }

    private IQueryable<Entities.Provider> ActiveProvidersFromAppDbQuery() =>
        _appDb.Providers
            .AsNoTracking()
            .Where(p => p.User.UserType == UserTypeConstants.Provider && p.User.IsActive);

    private IQueryable<ProviderProfile> ActiveProvidersQuery() =>
        _providerRepo.Query()
            .Where(p => p.UserU.UserType == UserTypeConstants.Provider && p.UserU.IsActive != false);

    private static string BuildAdminProviderEmail(string? mobileNo)
    {
        var key = string.IsNullOrWhiteSpace(mobileNo)
            ? Guid.NewGuid().ToString("N")[..12]
            : new string(mobileNo.Where(char.IsDigit).ToArray());

        return $"provider_{key}@homeservices.local";
    }

    private static ServiceProviderDetailsVm MapDetails(
        ProviderProfile provider,
        string categoryName,
        string? profilePath)
    {
        return new ServiceProviderDetailsVm
        {
            Uid = provider.Uid,
            FullName = provider.UserU.FullName ?? "—",
            MobileNo = provider.UserU.MobileNo,
            Cnic = provider.Cnic,
            CategoryName = categoryName,
            ExperienceYears = provider.ExperienceYears,
            Rating = provider.Rating,
            IsVerified = provider.IsVerified ?? false,
            IsActive = provider.UserU.IsActive ?? true,
            ProfilePicturePath = profilePath,
            CreatedOn = provider.UserU.CreatedOn
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
}

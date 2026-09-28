using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceProviderService : IServiceProviderService
{
    private const string CitiesConfigKey = "Cities";

    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfigurationEntryService _configurations;
    private readonly IProviderCategoryService _providerCategories;
    private readonly ILogger<ServiceProviderService> _logger;

    public ServiceProviderService(
        AppDbContext db,
        IFileStorageService fileStorage,
        IConfigurationEntryService configurations,
        IProviderCategoryService providerCategories,
        ILogger<ServiceProviderService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _configurations = configurations;
        _providerCategories = providerCategories;
        _logger = logger;
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

    public async Task<ServiceProviderFormVm> PopulateFormAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Categories = await GetCategoryOptionsAsync(cancellationToken);
        model.CityOptions = await BuildCityOptionsAsync(model.City, cancellationToken);
        if (model.Uid > 0)
        {
            await PopulateDocumentFormAsync(model, cancellationToken);
        }
        return model;
    }

    private async Task PopulateDocumentFormAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken)
    {
        var docs = await GetDocumentsByMobileAsync(model.MobileNo, cancellationToken);
        model.DocumentForm = new ProviderDocumentFormVm
        {
            Uid = docs?.Uid ?? 0,
            ProviderUid = model.Uid,
            ProviderName = model.FullName,
            MobileNo = model.MobileNo,
            ExistingProfilePhotoPath = docs?.ProfilePhotoPath,
            ExistingCnicFrontPath = docs?.CnicFrontImagePath,
            ExistingCnicBackPath = docs?.CnicBackImagePath,
            VerificationRemarks = docs?.VerificationRemarks
        };
    }

    private async Task<ServiceProviderDocumentTabVm?> GetDocumentsByMobileAsync(
        string? mobileNo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mobileNo))
        {
            return null;
        }

        return await _db.ProviderDocuments
            .AsNoTracking()
            .Where(d => d.MobileNo == mobileNo)
            .Select(d => new ServiceProviderDocumentTabVm
            {
                Uid = d.Uid,
                MobileNo = d.MobileNo,
                ProfilePhotoPath = d.ProfilePhotoPath,
                CnicFrontImagePath = d.CnicFrontImagePath,
                CnicBackImagePath = d.CnicBackImagePath,
                PoliceVerificationPath = d.PoliceVerificationPath,
                IsVerified = false, // display unused; status comes from Providers.IsVerified
                VerifiedOn = d.VerifiedOn,
                VerifiedBy = d.VerifiedBy,
                VerificationRemarks = d.VerificationRemarks,
                CreatedOn = d.CreatedOn,
                UpdatedOn = d.UpdatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<List<SelectListItem>> BuildCityOptionsAsync(
        string? currentCity,
        CancellationToken cancellationToken)
    {
        var values = await _configurations.GetValuesByKeyAsync(CitiesConfigKey, cancellationToken);
        var options = values
            .Select(v => new SelectListItem { Value = v, Text = v })
            .ToList();

        if (!string.IsNullOrWhiteSpace(currentCity)
            && !options.Any(o => string.Equals(o.Value, currentCity, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, new SelectListItem
            {
                Value = currentCity,
                Text = currentCity
            });
        }

        return options;
    }

    public async Task<ServiceProviderListVm> GetListAsync(
        string? search,
        string? verifyStatus,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "id" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var query = _db.Providers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.FullName.Contains(term) ||
                p.MobileNo.Contains(term) ||
                p.Cnic.Contains(term) ||
                p.Category.CategoryName.Contains(term));
        }

        if (string.Equals(verifyStatus, "1", StringComparison.Ordinal)
            || string.Equals(verifyStatus, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verifyStatus, "verified", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.IsVerified);
            verifyStatus = "1";
        }
        else if (string.Equals(verifyStatus, "0", StringComparison.Ordinal)
            || string.Equals(verifyStatus, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verifyStatus, "notverified", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verifyStatus, "not verified", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => !p.IsVerified);
            verifyStatus = "0";
        }
        else
        {
            verifyStatus = null;
        }

        query = sort switch
        {
            "id" or "uid" => sortDir == "desc"
                ? query.OrderByDescending(p => p.Uid)
                : query.OrderBy(p => p.Uid),
            "name" => sortDir == "desc"
                ? query.OrderByDescending(p => p.FullName)
                : query.OrderBy(p => p.FullName),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(p => p.CreatedOn)
                : query.OrderBy(p => p.CreatedOn),
            "category" => sortDir == "desc"
                ? query.OrderByDescending(p => p.Category.CategoryName)
                : query.OrderBy(p => p.Category.CategoryName),
            _ => sortDir == "desc"
                ? query.OrderByDescending(p => p.Uid)
                : query.OrderBy(p => p.Uid)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ServiceProviderItemVm
            {
                Uid = p.Uid,
                FullName = p.FullName,
                MobileNo = p.MobileNo,
                Cnic = p.Cnic,
                CategoryName = p.Category.CategoryName,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified,
                ProfilePicturePath = null,
                CreatedOn = p.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new ServiceProviderListVm
        {
            Items = items,
            Search = search,
            VerifyStatus = verifyStatus,
            Sort = sort,
            SortDir = sortDir,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ServiceProviderDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == id)
            .Select(p => new ServiceProviderDetailsVm
            {
                Uid = p.Uid,
                FullName = p.FullName,
                MobileNo = p.MobileNo,
                Cnic = p.Cnic,
                City = p.City,
                CategoryName = p.Category.CategoryName,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified,
                IsActive = p.User.IsActive,
                CreatedOn = p.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(provider.MobileNo))
        {
            provider.Documents = await GetDocumentsByMobileAsync(provider.MobileNo, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(provider.Documents?.ProfilePhotoPath))
        {
            var path = provider.Documents.ProfilePhotoPath.TrimStart('/');
            provider.ProfilePicturePath = "/" + path;
        }

        return provider;
    }

    public async Task<ServiceProviderFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == id)
            .Select(p => new ServiceProviderFormVm
            {
                Uid = p.Uid,
                FullName = p.FullName,
                MobileNo = p.MobileNo,
                Cnic = p.Cnic,
                City = p.City,
                CategoryUid = p.CategoryUid,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified,
                IsActive = p.User.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null) return null;

        provider.CategoryUids = await _providerCategories.GetCategoryUidsAsync(id, cancellationToken);
        if (provider.CategoryUids.Count == 0)
        {
            provider.CategoryUids = new List<int> { provider.CategoryUid };
        }

        return await PopulateFormAsync(provider, cancellationToken);
    }

    public async Task<ServiceProviderDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == id)
            .Select(p => new ServiceProviderDeleteVm
            {
                Uid = p.Uid,
                FullName = p.FullName,
                MobileNo = p.MobileNo,
                CategoryName = p.Category.CategoryName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null)
        {
            return null;
        }

        provider.DocumentCount = await _db.ProviderDocuments
            .CountAsync(d => d.ProviderUid == id, cancellationToken);
        provider.BookingCount = await _db.ServiceBookings
            .CountAsync(b => b.ProviderUid == id, cancellationToken);
        provider.PaymentLedgerCount = await _db.PaymentLedgers
            .CountAsync(p => p.ProviderUid == id, cancellationToken);
        provider.PayoutCount = await _db.ProviderPayouts
            .CountAsync(p => p.ProviderUid == id, cancellationToken);
        provider.CommissionRuleCount = await _db.CommissionRules
            .CountAsync(c => c.ProviderUid == id, cancellationToken);

        return provider;
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.MobileNo))
        {
            return (false, "Mobile number is required.");
        }

        if (string.IsNullOrWhiteSpace(model.Cnic))
        {
            return (false, "CNIC is required.");
        }

        var categoryExists = await _db.ServiceCategories
            .AnyAsync(c => c.Uid == model.CategoryUid && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return (false, "Selected category does not exist.");
        }

        var mobile = model.MobileNo.Trim();
        if (await _db.UsersLogins.AnyAsync(u => u.MobileNo == mobile, cancellationToken))
        {
            return (false, "A user with this mobile number already exists.");
        }

        var user = new UsersLogin
        {
            MobileNo = mobile,
            PasswordHash = PasswordHasher.Hash(model.Password),
            UserType = UserTypeConstants.Provider,
            IsActive = model.IsActive,
            IsVerified = false,
            CreatedOn = DateTime.UtcNow
        };

        _db.UsersLogins.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var provider = new Provider
        {
            UserUid = user.Uid,
            MobileNo = mobile,
            FullName = model.FullName.Trim(),
            Cnic = model.Cnic.Trim(),
            City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
            ExperienceYears = model.ExperienceYears ?? 0,
            IsVerified = model.IsVerified,
            AverageRating = model.Rating ?? 0,
            CategoryUid = model.CategoryUid,
            IsAvailable = true,
            CreatedOn = DateTime.UtcNow
        };

        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(cancellationToken);

        var categoryUids = model.CategoryUids is { Count: > 0 } ? model.CategoryUids : new List<int> { model.CategoryUid };
        var (syncSuccess, syncError) = await _providerCategories.SyncCategoriesAsync(
            provider.Uid, categoryUids, model.CategoryUid, cancellationToken);
        if (!syncSuccess)
        {
            return (false, syncError);
        }

        _logger.LogInformation("Provider {Name} created.", model.FullName);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceProviderFormVm model,
        CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Uid == model.Uid, cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.");
        }

        if (string.IsNullOrWhiteSpace(model.MobileNo))
        {
            return (false, "Mobile number is required.");
        }

        if (string.IsNullOrWhiteSpace(model.Cnic))
        {
            return (false, "CNIC is required.");
        }

        var categoryExists = await _db.ServiceCategories
            .AnyAsync(c => c.Uid == model.CategoryUid && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return (false, "Selected category does not exist.");
        }

        var mobile = model.MobileNo.Trim();
        var mobileTaken = await _db.UsersLogins
            .AnyAsync(u => u.MobileNo == mobile && u.Uid != provider.UserUid, cancellationToken);

        if (mobileTaken)
        {
            return (false, "A user with this mobile number already exists.");
        }

        var providerMobileTaken = await _db.Providers
            .AnyAsync(p => p.MobileNo == mobile && p.Uid != provider.Uid, cancellationToken);
        if (providerMobileTaken)
        {
            return (false, "A provider with this mobile number already exists.");
        }

        provider.FullName = model.FullName.Trim();
        provider.Cnic = model.Cnic.Trim();
        provider.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        provider.ExperienceYears = model.ExperienceYears ?? 0;
        provider.AverageRating = model.Rating ?? provider.AverageRating;
        provider.IsVerified = model.IsVerified;
        provider.User.MobileNo = mobile;
        provider.MobileNo = mobile; // ON UPDATE CASCADE syncs ProviderDocuments.MobileNo
        provider.User.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        var categoryUids = model.CategoryUids is { Count: > 0 } ? model.CategoryUids : new List<int> { model.CategoryUid };
        var (syncSuccess, syncError) = await _providerCategories.SyncCategoriesAsync(
            provider.Uid, categoryUids, model.CategoryUid, cancellationToken);
        if (!syncSuccess)
        {
            return (false, syncError);
        }

        _logger.LogInformation("Provider {Uid} updated.", model.Uid);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var provider = await _db.Providers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Uid == id, cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.");
        }

        try
        {
            var bookingIds = await _db.ServiceBookings
                .Where(b => b.ProviderUid == id)
                .Select(b => b.Uid)
                .ToListAsync(cancellationToken);

            if (bookingIds.Count > 0)
            {
                var bookingLedger = await _db.PaymentLedgers
                    .Where(p => p.BookingUid != null && bookingIds.Contains(p.BookingUid.Value))
                    .ToListAsync(cancellationToken);
                if (bookingLedger.Count > 0)
                {
                    _db.PaymentLedgers.RemoveRange(bookingLedger);
                }

                var bookings = await _db.ServiceBookings
                    .Where(b => b.ProviderUid == id)
                    .ToListAsync(cancellationToken);
                _db.ServiceBookings.RemoveRange(bookings);
            }

            var providerLedger = await _db.PaymentLedgers
                .Where(p => p.ProviderUid == id)
                .ToListAsync(cancellationToken);
            if (providerLedger.Count > 0)
            {
                _db.PaymentLedgers.RemoveRange(providerLedger);
            }

            var payouts = await _db.ProviderPayouts
                .Where(p => p.ProviderUid == id)
                .ToListAsync(cancellationToken);
            if (payouts.Count > 0)
            {
                _db.ProviderPayouts.RemoveRange(payouts);
            }

            var commissionRules = await _db.CommissionRules
                .Where(c => c.ProviderUid == id)
                .ToListAsync(cancellationToken);
            if (commissionRules.Count > 0)
            {
                _db.CommissionRules.RemoveRange(commissionRules);
            }

            var documents = await _db.ProviderDocuments
                .Where(d => d.ProviderUid == id)
                .ToListAsync(cancellationToken);
            if (documents.Count > 0)
            {
                _db.ProviderDocuments.RemoveRange(documents);
            }

            var userUid = provider.UserUid;
            var loginStillLinked = await _db.Clients.AnyAsync(c => c.UserUid == userUid, cancellationToken)
                || await _db.Staff.AnyAsync(s => s.UserUid == userUid, cancellationToken);

            _db.Providers.Remove(provider);

            // Keep UsersLogin when the same account is still used as a client or staff member.
            if (!loginStillLinked && provider.User != null)
            {
                _db.UsersLogins.Remove(provider.User);
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (documents.Count > 0)
            {
                _fileStorage.DeleteProviderDocumentFiles(id);
            }

            _logger.LogInformation(
                "Provider {Uid} deleted (docs={Docs}, bookings={Bookings}, ledger={Ledger}, payouts={Payouts}, rules={Rules}).",
                id,
                documents.Count,
                bookingIds.Count,
                providerLedger.Count,
                payouts.Count,
                commissionRules.Count);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this provider because related records still reference their account.");
        }
    }
}

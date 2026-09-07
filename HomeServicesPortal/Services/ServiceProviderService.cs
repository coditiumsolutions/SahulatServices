using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceProviderService : IServiceProviderService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ServiceProviderService> _logger;

    public ServiceProviderService(
        AppDbContext db,
        IFileStorageService fileStorage,
        ILogger<ServiceProviderService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
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

        var query = _db.Providers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.FullName.Contains(term) ||
                p.User.MobileNo.Contains(term) ||
                p.Cnic.Contains(term) ||
                p.Category.CategoryName.Contains(term));
        }

        query = sort switch
        {
            "category" => sortDir == "desc"
                ? query.OrderByDescending(p => p.Category.CategoryName)
                : query.OrderBy(p => p.Category.CategoryName),
            "rating" => sortDir == "desc"
                ? query.OrderByDescending(p => p.AverageRating)
                : query.OrderBy(p => p.AverageRating),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(p => p.CreatedOn)
                : query.OrderBy(p => p.CreatedOn),
            _ => sortDir == "desc"
                ? query.OrderByDescending(p => p.FullName)
                : query.OrderBy(p => p.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ServiceProviderItemVm
            {
                Uid = p.Uid,
                FullName = p.FullName,
                MobileNo = p.User.MobileNo,
                Cnic = p.Cnic,
                CategoryName = p.Category.CategoryName,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified,
                IsActive = p.User.IsActive,
                ProfilePicturePath = null,
                CreatedOn = p.CreatedOn
            })
            .ToListAsync(cancellationToken);

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
        return await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == id)
            .Select(p => new ServiceProviderDetailsVm
            {
                Uid = p.Uid,
                FullName = p.FullName,
                MobileNo = p.User.MobileNo,
                Cnic = p.Cnic,
                CategoryName = p.Category.CategoryName,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified,
                IsActive = p.User.IsActive,
                ProfilePicturePath = null,
                CreatedOn = p.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
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
                MobileNo = p.User.MobileNo,
                Cnic = p.Cnic,
                CategoryUid = p.CategoryUid,
                ExperienceYears = p.ExperienceYears,
                Rating = p.AverageRating,
                IsVerified = p.IsVerified,
                IsActive = p.User.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null) return null;

        provider.Categories = await GetCategoryOptionsAsync(cancellationToken);
        return provider;
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
                MobileNo = p.User.MobileNo,
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
            PasswordHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N")[..8]),
            UserType = UserTypeConstants.Provider,
            IsActive = model.IsActive,
            IsVerified = false,
            CreatedOn = DateTime.Now
        };

        _db.UsersLogins.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _db.Providers.Add(new Provider
        {
            UserUid = user.Uid,
            FullName = model.FullName.Trim(),
            Cnic = model.Cnic.Trim(),
            ExperienceYears = model.ExperienceYears ?? 0,
            IsVerified = model.IsVerified,
            AverageRating = model.Rating ?? 0,
            CategoryUid = model.CategoryUid,
            IsAvailable = true,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
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

        provider.FullName = model.FullName.Trim();
        provider.Cnic = model.Cnic.Trim();
        provider.CategoryUid = model.CategoryUid;
        provider.ExperienceYears = model.ExperienceYears ?? 0;
        provider.AverageRating = model.Rating ?? provider.AverageRating;
        provider.IsVerified = model.IsVerified;
        provider.User.MobileNo = mobile;
        provider.User.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
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

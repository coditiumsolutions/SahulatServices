using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api.Auth;
using HomeServicesPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services.Auth;

public class AuthService : IAuthService
{
    public const string RoleCustomer = "Customer";
    public const string RoleProvider = "Provider";
    public const string RoleAdmin = "Admin";

    private readonly SahulatAppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        SahulatAppDbContext db,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<(bool Success, string? Error, LoginResponseDto? Data)> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var login = request.EmailOrPhone.Trim();
        var user = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                (u.Email != null && u.Email.ToLower() == login.ToLower()) ||
                (u.MobileNo != null && u.MobileNo == login),
                cancellationToken);

        if (user == null || user.IsActive == false)
        {
            return (false, "Invalid email/phone or password.", null);
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash != request.Password)
        {
            return (false, "Invalid email/phone or password.", null);
        }

        var role = NormalizeRole(user.UserType);
        var username = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName! : user.Email ?? login;
        var (token, expiresAt) = _jwtTokenService.CreateToken(user.Uid, user.Email ?? login, role);

        return (true, null, new LoginResponseDto
        {
            UserId = user.Uid,
            Username = username,
            Role = role,
            Token = token,
            ExpiresAt = expiresAt
        });
    }

    public async Task<(bool Success, string? Error, RegisterCustomerResponseDto? Data)> RegisterCustomerAsync(
        RegisterCustomerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await EmailExistsAsync(email, cancellationToken))
        {
            return (false, "An account with this email already exists.", null);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new AppUser
            {
                FullName = request.FullName.Trim(),
                Email = email,
                MobileNo = request.Phone?.Trim(),
                UserType = RoleCustomer,
                IsActive = true,
                CreatedOn = DateTime.Now
            };
            user.PasswordHash = request.Password;

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var profile = new CustomerProfile
            {
                UserUid = user.Uid,
                DefaultAddress = request.DefaultAddress?.Trim()
            };

            _db.CustomerProfiles.Add(profile);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (true, null, new RegisterCustomerResponseDto
            {
                UserId = user.Uid,
                ProfileId = profile.Uid,
                Email = user.Email!,
                Role = RoleCustomer
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string? Error, RegisterProviderResponseDto? Data)> RegisterProviderAsync(
        RegisterProviderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await EmailExistsAsync(email, cancellationToken))
        {
            return (false, "An account with this email already exists.", null);
        }

        var (categoryId, categoryName, categoryError) = await ResolveCategoryAsync(
            request.CategoryId,
            request.ServiceType,
            cancellationToken);

        if (categoryError != null)
        {
            return (false, categoryError, null);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new AppUser
            {
                FullName = request.FullName.Trim(),
                Email = email,
                MobileNo = request.Phone?.Trim(),
                UserType = RoleProvider,
                IsActive = true,
                CreatedOn = DateTime.Now
            };
            user.PasswordHash = request.Password;

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var profile = new ProviderProfile
            {
                UserUid = user.Uid,
                CategoryUid = categoryId,
                Cnic = request.Cnic?.Trim(),
                ExperienceYears = request.ExperienceYears ?? 0,
                Rating = 0,
                IsVerified = false
            };

            _db.ProviderProfiles.Add(profile);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (true, null, new RegisterProviderResponseDto
            {
                UserId = user.Uid,
                ProfileId = profile.Uid,
                Email = user.Email!,
                Role = RoleProvider,
                CategoryId = categoryId,
                ServiceType = categoryName
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await _db.AppUsers.AnyAsync(
            u => u.Email != null && u.Email.ToLower() == email,
            cancellationToken);
    }

    private async Task<(int? CategoryId, string? CategoryName, string? Error)> ResolveCategoryAsync(
        int? categoryId,
        string? serviceType,
        CancellationToken cancellationToken)
    {
        if (categoryId.HasValue)
        {
            var byId = await _db.ServiceCategories
                .AsNoTracking()
                .Where(c => c.Uid == categoryId.Value && c.IsActive != false)
                .Select(c => new { c.Uid, c.CategoryName })
                .FirstOrDefaultAsync(cancellationToken);

            if (byId == null)
            {
                return (null, null, "Invalid service category id.");
            }

            return (byId.Uid, byId.CategoryName, null);
        }

        if (!string.IsNullOrWhiteSpace(serviceType))
        {
            var term = serviceType.Trim();
            var byName = await _db.ServiceCategories
                .AsNoTracking()
                .Where(c => c.IsActive != false && c.CategoryName.ToLower() == term.ToLower())
                .Select(c => new { c.Uid, c.CategoryName })
                .FirstOrDefaultAsync(cancellationToken);

            if (byName == null)
            {
                return (null, null, $"Service type '{term}' was not found. Use a valid category name or CategoryId.");
            }

            return (byName.Uid, byName.CategoryName, null);
        }

        return (null, null, "CategoryId or ServiceType is required for provider registration.");
    }

    private static string NormalizeRole(string? userType)
    {
        if (string.IsNullOrWhiteSpace(userType))
        {
            return RoleCustomer;
        }

        var normalized = userType.Trim();
        if (normalized.Equals(RoleProvider, StringComparison.OrdinalIgnoreCase))
        {
            return RoleProvider;
        }

        if (normalized.Equals(RoleAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return RoleAdmin;
        }

        if (normalized.Equals(RoleCustomer, StringComparison.OrdinalIgnoreCase))
        {
            return RoleCustomer;
        }

        return normalized;
    }
}

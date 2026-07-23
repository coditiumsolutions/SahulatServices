using AutoMapper;
using HomeServicesPortal.Data;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public AuthService(
        AppDbContext db,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _db = db;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<(bool Success, string? Error, RegistrationResponse? Data)> RegisterClientAsync(
        RegisterClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!mobileOk)
        {
            return (false, mobileError, null);
        }

        var existing = await _userRepository.GetUserByMobileAsync(mobileNo, cancellationToken);
        if (existing is { IsVerified: true })
        {
            return (false, "Mobile number already registered.", null);
        }

        if (existing != null)
        {
            // Incomplete signup: refresh credentials/profile, keep IsVerified = false until OTP verify
            return await ExecuteInTransactionAsync<(bool Success, string? Error, RegistrationResponse? Data)>(async () =>
            {
                existing.PasswordHash = PasswordHasher.Hash(request.Password);
                existing.UserType = UserTypeConstants.Client;
                existing.IsActive = true;
                existing.IsVerified = false;
                await _userRepository.SaveChangesAsync(cancellationToken);

                var client = await _userRepository.GetClientByUserIdAsync(existing.Uid, cancellationToken);
                if (client == null)
                {
                    client = new Client
                    {
                        UserUid = existing.Uid,
                        FullName = request.FullName.Trim(),
                        Cnic = request.CNIC?.Trim(),
                        Gender = request.Gender?.Trim(),
                        CreatedOn = DateTime.Now
                    };
                    await _userRepository.CreateClientAsync(client, cancellationToken);
                }
                else
                {
                    var tracked = await _db.Clients.FirstAsync(c => c.Uid == client.Uid, cancellationToken);
                    tracked.FullName = request.FullName.Trim();
                    tracked.Cnic = request.CNIC?.Trim();
                    tracked.Gender = request.Gender?.Trim();
                    await _db.SaveChangesAsync(cancellationToken);
                    client = tracked;
                }

                return (true, null, _mapper.Map<RegistrationResponse>((existing, client)));
            }, cancellationToken);
        }

        return await ExecuteInTransactionAsync<(bool Success, string? Error, RegistrationResponse? Data)>(async () =>
        {
            var user = new UsersLogin
            {
                MobileNo = mobileNo,
                PasswordHash = PasswordHasher.Hash(request.Password),
                UserType = UserTypeConstants.Client,
                IsActive = true,
                IsVerified = false,
                CreatedOn = DateTime.Now
            };

            await _userRepository.CreateUserAsync(user, cancellationToken);

            var client = new Client
            {
                UserUid = user.Uid,
                FullName = request.FullName.Trim(),
                Cnic = request.CNIC?.Trim(),
                Gender = request.Gender?.Trim(),
                CreatedOn = DateTime.Now
            };

            await _userRepository.CreateClientAsync(client, cancellationToken);
            return (true, null, _mapper.Map<RegistrationResponse>((user, client)));
        }, cancellationToken);
    }

    public async Task<(bool Success, string? Error, ProviderUpgradeResponse? Data, int StatusCode)> RegisterProviderAsync(
        RegisterProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!mobileOk)
        {
            return (false, mobileError, null, StatusCodes.Status400BadRequest);
        }

        var user = await _userRepository.GetUserByMobileAsync(mobileNo, cancellationToken);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return (false, "Invalid mobile number or password.", null, StatusCodes.Status401Unauthorized);
        }

        if (!user.IsVerified)
        {
            return (false, "Mobile number is not verified. Please verify OTP first.", null, StatusCodes.Status403Forbidden);
        }

        var userId = user.Uid;

        if (!user.IsActive)
        {
            return (false, "Account is inactive. Contact support.", null, StatusCodes.Status403Forbidden);
        }

        if (user.UserType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Account is already registered as a provider.", null, StatusCodes.Status409Conflict);
        }

        if (!user.UserType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Only client accounts can be upgraded to provider.", null, StatusCodes.Status403Forbidden);
        }

        if (await _userRepository.ProviderExistsForUserAsync(userId, cancellationToken))
        {
            return (false, "Provider profile already exists for this account.", null, StatusCodes.Status409Conflict);
        }

        var cnic = request.CNIC.Trim();
        if (await _userRepository.ProviderCnicExistsAsync(cnic, cancellationToken))
        {
            return (false, "CNIC is already registered to another provider.", null, StatusCodes.Status409Conflict);
        }

        var client = await _userRepository.GetClientByUserIdAsync(userId, cancellationToken);
        if (client == null)
        {
            return (false, "Client profile not found.", null, StatusCodes.Status404NotFound);
        }

        var (categoryId, categoryName, categoryError) = await ResolveCategoryAsync(
            request.CategoryId,
            request.CategoryName,
            cancellationToken);

        if (categoryError != null)
        {
            return (false, categoryError, null, StatusCodes.Status400BadRequest);
        }

        var fullName = !string.IsNullOrWhiteSpace(request.FullName)
            ? request.FullName.Trim()
            : client.FullName;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (false, "Full name is required.", null, StatusCodes.Status400BadRequest);
        }

        return await ExecuteInTransactionAsync<(bool Success, string? Error, ProviderUpgradeResponse? Data, int StatusCode)>(async () =>
        {
            await _userRepository.UpdateUserTypeAsync(userId, UserTypeConstants.Provider, cancellationToken);

            var provider = new Provider
            {
                UserUid = userId,
                FullName = fullName,
                Cnic = cnic,
                Gender = request.Gender?.Trim() ?? client.Gender,
                ExperienceYears = request.ExperienceYears ?? 0,
                Description = request.Description?.Trim(),
                CategoryUid = categoryId!.Value,
                IsVerified = false,
                AverageRating = 0,
                TotalReviews = 0,
                TotalJobsCompleted = 0,
                IsAvailable = true,
                CreatedOn = DateTime.Now
            };

            await _userRepository.CreateProviderAsync(provider, cancellationToken);
            return (true, null, new ProviderUpgradeResponse
            {
                UserId = userId,
                ProfileId = provider.Uid,
                ProviderUid = provider.Uid,
                UserType = UserTypeConstants.Provider,
                FullName = fullName,
                MobileNo = user.MobileNo,
                CategoryId = categoryId,
                CategoryName = categoryName
            }, StatusCodes.Status200OK);
        }, cancellationToken);
    }

    private async Task<(int? CategoryId, string? CategoryName, string? Error)> ResolveCategoryAsync(
        int? categoryId,
        string? categoryName,
        CancellationToken cancellationToken)
    {
        if (categoryId.HasValue)
        {
            var byId = await _db.ServiceCategories
                .AsNoTracking()
                .Where(c => c.Uid == categoryId.Value && c.IsActive)
                .Select(c => new { c.Uid, c.CategoryName })
                .FirstOrDefaultAsync(cancellationToken);

            if (byId == null)
            {
                return (null, null, "Invalid or inactive service category id.");
            }

            return (byId.Uid, byId.CategoryName, null);
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var term = categoryName.Trim();
            var byName = await _db.ServiceCategories
                .AsNoTracking()
                .Where(c => c.IsActive && c.CategoryName.ToLower() == term.ToLower())
                .Select(c => new { c.Uid, c.CategoryName })
                .FirstOrDefaultAsync(cancellationToken);

            if (byName == null)
            {
                return (null, null, $"Service category '{term}' was not found.");
            }

            return (byName.Uid, byName.CategoryName, null);
        }

        return (null, null, "CategoryId or CategoryName is required.");
    }

    public async Task<(bool Success, string? Error, RegistrationResponse? Data)> RegisterStaffAsync(
        RegisterStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!mobileOk)
        {
            return (false, mobileError, null);
        }

        if (await _userRepository.MobileExistsAsync(mobileNo, cancellationToken))
        {
            return (false, "Mobile number already registered.", null);
        }

        return await ExecuteInTransactionAsync<(bool Success, string? Error, RegistrationResponse? Data)>(async () =>
        {
            var user = new UsersLogin
            {
                MobileNo = mobileNo,
                PasswordHash = PasswordHasher.Hash(request.Password),
                UserType = UserTypeConstants.Staff,
                IsActive = true,
                IsVerified = true, // Staff accounts are portal-managed; OTP not required
                CreatedOn = DateTime.Now
            };

            await _userRepository.CreateUserAsync(user, cancellationToken);

            var staff = new Staff
            {
                UserUid = user.Uid,
                FullName = request.FullName.Trim(),
                Designation = request.Designation?.Trim(),
                Department = request.Department?.Trim(),
                IsAdmin = request.IsAdmin,
                CreatedOn = DateTime.Now
            };

            await _userRepository.CreateStaffAsync(staff, cancellationToken);
            return (true, null, _mapper.Map<RegistrationResponse>((user, staff)));
        }, cancellationToken);
    }

    public async Task<(bool Success, string? Error, LoginResponse? Data, int StatusCode)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!mobileOk)
        {
            return (false, mobileError, null, StatusCodes.Status400BadRequest);
        }

        var user = await _userRepository.GetUserByMobileAsync(mobileNo, cancellationToken);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return (false, "Invalid mobile number or password.", null, StatusCodes.Status401Unauthorized);
        }

        if (!user.IsActive)
        {
            return (false, "Account is inactive. Contact support.", null, StatusCodes.Status403Forbidden);
        }

        // Clients and Providers must verify OTP before login. Staff accounts are portal-managed.
        if (!user.IsVerified
            && !user.UserType.Equals(UserTypeConstants.Staff, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Mobile number is not verified. Please verify OTP first.", null, StatusCodes.Status403Forbidden);
        }

        if (!UserTypeConstants.IsValid(user.UserType))
        {
            return (false, "Invalid user type on account.", null, StatusCodes.Status403Forbidden);
        }

        var (profileId, fullName) = await ResolveProfileAsync(user, cancellationToken);
        if (profileId == 0 || string.IsNullOrWhiteSpace(fullName))
        {
            return (false, "User profile not found.", null, StatusCodes.Status404NotFound);
        }

        await _userRepository.UpdateLastLoginAsync(user.Uid, DateTime.Now, cancellationToken);

        return (true, null, new LoginResponse
        {
            UserId = user.Uid,
            ProfileId = profileId,
            UserType = user.UserType,
            FullName = fullName,
            MobileNo = user.MobileNo
        }, StatusCodes.Status200OK);
    }

    private async Task<(int ProfileId, string FullName)> ResolveProfileAsync(
        UsersLogin user,
        CancellationToken cancellationToken)
    {
        if (user.UserType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase))
        {
            var client = await _userRepository.GetClientByUserIdAsync(user.Uid, cancellationToken);
            return client == null ? (0, string.Empty) : (client.Uid, client.FullName);
        }

        if (user.UserType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            var provider = await _userRepository.GetProviderByUserIdAsync(user.Uid, cancellationToken);
            return provider == null ? (0, string.Empty) : (provider.Uid, provider.FullName);
        }

        if (user.UserType.Equals(UserTypeConstants.Staff, StringComparison.OrdinalIgnoreCase))
        {
            var staff = await _userRepository.GetStaffByUserIdAsync(user.Uid, cancellationToken);
            return staff == null ? (0, string.Empty) : (staff.Uid, staff.FullName);
        }

        return (0, string.Empty);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}

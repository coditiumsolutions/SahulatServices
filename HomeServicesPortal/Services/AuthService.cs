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
    private readonly IFileStorageService _fileStorageService;

    public AuthService(
        AppDbContext db,
        IUserRepository userRepository,
        IMapper mapper,
        IFileStorageService fileStorageService)
    {
        _db = db;
        _userRepository = userRepository;
        _mapper = mapper;
        _fileStorageService = fileStorageService;
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

    /// <summary>
    /// Permanently deletes a Client's and/or Provider's account and personal data.
    /// Rows with FK-restricted history (CustomerServiceRequests, ServiceBookings, PaymentLedger,
    /// ProviderPayouts, CommissionRules) cannot be hard-deleted without breaking other users'
    /// booking/financial history, so their PII columns are anonymized instead. Rows with no such
    /// dependents (ClientAddresses with no requests, ProviderDocuments, UserOTP) are hard-deleted,
    /// along with the provider's uploaded document files on disk.
    /// </summary>
    public async Task<(bool Success, string? Error, DeleteAccountResponse? Data, int StatusCode)> DeleteAccountAsync(
        DeleteAccountRequest request,
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

        var userId = user.Uid;
        var client = await _userRepository.GetClientByUserIdAsync(userId, cancellationToken);
        var provider = await _userRepository.GetProviderByUserIdAsync(userId, cancellationToken);

        if (client == null && provider == null)
        {
            return (false, "Account not found.", null, StatusCodes.Status404NotFound);
        }

        return await ExecuteInTransactionAsync<(bool Success, string? Error, DeleteAccountResponse? Data, int StatusCode)>(async () =>
        {
            var anonymizedName = $"Deleted User {userId}";
            // MobileNo column is nvarchar(20) and unique. userId alone guarantees uniqueness
            // (UsersLogin.Uid is the PK), so it is sufficient without a timestamp suffix.
            var anonymizedMobile = $"deleted-{userId}";

            if (client != null)
            {
                var addresses = await _db.ClientAddresses
                    .Where(a => a.ClientUid == client.Uid)
                    .ToListAsync(cancellationToken);

                foreach (var address in addresses)
                {
                    var hasRequests = await _db.CustomerServiceRequests
                        .AnyAsync(r => r.ClientAddressUid == address.Uid, cancellationToken);
                    if (!hasRequests)
                    {
                        _db.ClientAddresses.Remove(address);
                    }
                }

                var trackedClient = await _db.Clients.FirstAsync(c => c.Uid == client.Uid, cancellationToken);
                trackedClient.FullName = anonymizedName;
                trackedClient.Cnic = null;
                trackedClient.Gender = null;
            }

            if (provider != null)
            {
                var document = await _db.ProviderDocuments
                    .FirstOrDefaultAsync(d => d.ProviderUid == provider.Uid, cancellationToken);
                if (document != null)
                {
                    _db.ProviderDocuments.Remove(document);
                }

                var trackedProvider = await _db.Providers.FirstAsync(p => p.Uid == provider.Uid, cancellationToken);
                trackedProvider.FullName = anonymizedName;
                // Cnic column is nvarchar(15) and required (not nullable) — keep this short.
                trackedProvider.Cnic = $"DEL{provider.Uid}";
                trackedProvider.Gender = null;
                trackedProvider.Description = null;
                trackedProvider.IsAvailable = false;
            }

            var otpRows = await _db.UserOTPs
                .Where(o => o.MobileNo == mobileNo)
                .ToListAsync(cancellationToken);
            _db.UserOTPs.RemoveRange(otpRows);

            var trackedUser = await _db.UsersLogins.FirstAsync(u => u.Uid == userId, cancellationToken);
            trackedUser.MobileNo = anonymizedMobile;
            trackedUser.PasswordHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N"));
            trackedUser.IsActive = false;
            trackedUser.IsVerified = false;

            await _db.SaveChangesAsync(cancellationToken);

            if (provider != null)
            {
                _fileStorageService.DeleteProviderDocumentFiles(provider.Uid);
            }

            return (true, null, new DeleteAccountResponse { MobileNo = mobileNo }, StatusCodes.Status200OK);
        }, cancellationToken);
    }

    /// <summary>
    /// Atomically verifies a PasswordReset OTP and sets the new password in the same call
    /// (mirrors DeleteAccountAsync re-verifying identity in the same call as the sensitive
    /// action, rather than trusting a "verified" flag set by an earlier call).
    /// </summary>
    public async Task<(bool Success, string? Error, ResetPasswordResponse? Data, int StatusCode)> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!mobileOk)
        {
            return (false, mobileError, null, StatusCodes.Status400BadRequest);
        }

        // Scoped to OTPType == PasswordReset explicitly (unlike VerifyOtpAsync, which matches
        // the latest unverified OTP of any type) so a pending Login/Registration OTP can't be
        // used to reset the password.
        var otpRow = await _db.UserOTPs
            .Where(o => o.MobileNo == mobileNo && o.OTPType == OtpTypeConstants.PasswordReset && !o.IsVerified)
            .OrderByDescending(o => o.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (otpRow == null)
        {
            return (false, "No password reset request found for this mobile number. Please request a new OTP.", null, StatusCodes.Status404NotFound);
        }

        if (otpRow.AttemptCount >= OtpService.MaxVerifyAttempts)
        {
            return (false, "Maximum attempts exceeded.", null, StatusCodes.Status429TooManyRequests);
        }

        if (otpRow.ExpiryTime < DateTime.Now)
        {
            return (false, "OTP expired.", null, StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(otpRow.OTPCode, request.OTP.Trim(), StringComparison.Ordinal))
        {
            otpRow.AttemptCount += 1;
            await _db.SaveChangesAsync(cancellationToken);

            if (otpRow.AttemptCount >= OtpService.MaxVerifyAttempts)
            {
                return (false, "Maximum attempts exceeded.", null, StatusCodes.Status429TooManyRequests);
            }

            return (false, "Invalid OTP.", null, StatusCodes.Status400BadRequest);
        }

        var user = await _userRepository.GetUserByMobileAsync(mobileNo, cancellationToken);
        if (user == null)
        {
            return (false, "Account not found.", null, StatusCodes.Status404NotFound);
        }

        otpRow.IsVerified = true;
        otpRow.VerifiedOn = DateTime.Now;
        otpRow.AttemptCount = 0;

        // Invalidate any other pending PasswordReset OTPs for this mobile number so an old
        // code can't be replayed after this one succeeds.
        var otherPending = await _db.UserOTPs
            .Where(o => o.MobileNo == mobileNo
                        && o.OTPType == OtpTypeConstants.PasswordReset
                        && !o.IsVerified
                        && o.Uid != otpRow.Uid)
            .ToListAsync(cancellationToken);
        _db.UserOTPs.RemoveRange(otherPending);

        var trackedUser = await _db.UsersLogins.FirstAsync(u => u.Uid == user.Uid, cancellationToken);
        trackedUser.PasswordHash = PasswordHasher.Hash(request.NewPassword);

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, new ResetPasswordResponse { MobileNo = mobileNo }, StatusCodes.Status200OK);
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

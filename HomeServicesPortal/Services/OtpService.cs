using System.Security.Cryptography;
using HomeServicesPortal.Data;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Services;

public class OtpService : IOtpService
{
    public const int OtpExpiryMinutes = 5;
    public const int MaxVerifyAttempts = 3;
    public const int MaxResendCount = 5;

    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly OtpOptions _otpOptions;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        AppDbContext db,
        ISmsService smsService,
        IOptions<OtpOptions> otpOptions,
        ILogger<OtpService> logger)
    {
        _db = db;
        _smsService = smsService;
        _otpOptions = otpOptions.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, SendOtpResponse? Data, int StatusCode)> SendOtpAsync(
        SendOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var (isValid, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!isValid)
        {
            return (false, mobileError, null, StatusCodes.Status400BadRequest);
        }

        var otpType = OtpTypeConstants.Normalize(request.OTPType);

        if (otpType == OtpTypeConstants.Registration)
        {
            var existingUser = await _db.UsersLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MobileNo == mobileNo, cancellationToken);

            if (existingUser is { IsVerified: true })
            {
                return (false, "Mobile number already registered.", null, StatusCodes.Status409Conflict);
            }
        }
        else if (otpType == OtpTypeConstants.PasswordReset)
        {
            // Opposite of Registration's check: there must already be a verified account to reset.
            var existingUser = await _db.UsersLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MobileNo == mobileNo, cancellationToken);

            if (existingUser is not { IsVerified: true })
            {
                return (false, "Account not found.", null, StatusCodes.Status404NotFound);
            }
        }

        var otpCode = GenerateSixDigitOtp();
        var expiry = DateTime.Now.AddMinutes(OtpExpiryMinutes);

        // SERIALIZABLE so a double-tap on "Send OTP" (plausible on a flaky mobile connection) can't
        // have both calls see no pending row and both Add a new UserOTP row (breaking the "latest
        // OTP" OrderByDescending(CreatedOn).FirstOrDefault assumption used by VerifyOtpAsync/
        // ResetPasswordAsync), nor both update the same row's SentCount non-atomically. Reuses an
        // ambient transaction if one is already open on this scoped context (ResendOtpAsync calls
        // into this method) rather than nesting — EF Core doesn't support nested transactions.
        // Wrapped in CreateExecutionStrategy().ExecuteAsync when starting a new transaction, since
        // EnableRetryOnFailure is on in Development and a bare BeginTransactionAsync throws under
        // SqlServerRetryingExecutionStrategy — confirmed by testing this locally.
        if (_db.Database.CurrentTransaction != null)
        {
            await SaveOtpAsync(mobileNo, otpType, otpCode, expiry, cancellationToken);
        }
        else
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);
                await SaveOtpAsync(mobileNo, otpType, otpCode, expiry, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }

        await _smsService.SendOtpAsync(mobileNo, otpCode, cancellationToken);
        _logger.LogInformation("OTP sent for {MobileNo} type {OtpType}", mobileNo, otpType);

        return (true, null, BuildSendResponse(mobileNo, otpType, expiry, otpCode), StatusCodes.Status200OK);
    }

    private async Task SaveOtpAsync(
        string mobileNo,
        string otpType,
        string otpCode,
        DateTime expiry,
        CancellationToken cancellationToken)
    {
        var pending = await _db.UserOTPs
            .Where(o => o.MobileNo == mobileNo
                        && o.OTPType == otpType
                        && !o.IsVerified)
            .OrderByDescending(o => o.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending != null)
        {
            pending.OTPCode = otpCode;
            pending.ExpiryTime = expiry;
            pending.AttemptCount = 0;
            pending.SentCount += 1;
            pending.VerifiedOn = null;
        }
        else
        {
            _db.UserOTPs.Add(new UserOTP
            {
                MobileNo = mobileNo,
                OTPCode = otpCode,
                OTPType = otpType,
                ExpiryTime = expiry,
                IsVerified = false,
                AttemptCount = 0,
                SentCount = 1,
                CreatedOn = DateTime.Now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error, VerifyOtpResponse? Data, int StatusCode)> VerifyOtpAsync(
        VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var (isValid, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!isValid)
        {
            return (false, mobileError, null, StatusCodes.Status400BadRequest);
        }

        var otpInput = request.OTP.Trim();

        var otpRow = await _db.UserOTPs
            .Where(o => o.MobileNo == mobileNo && !o.IsVerified)
            .OrderByDescending(o => o.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (otpRow == null)
        {
            return (false, "OTP not found.", null, StatusCodes.Status404NotFound);
        }

        if (otpRow.AttemptCount >= MaxVerifyAttempts)
        {
            return (false, "Maximum attempts exceeded.", null, StatusCodes.Status429TooManyRequests);
        }

        if (otpRow.ExpiryTime < DateTime.Now)
        {
            return (false, "OTP expired.", null, StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(otpRow.OTPCode, otpInput, StringComparison.Ordinal))
        {
            otpRow.AttemptCount += 1;
            await _db.SaveChangesAsync(cancellationToken);

            if (otpRow.AttemptCount >= MaxVerifyAttempts)
            {
                return (false, "Maximum attempts exceeded.", null, StatusCodes.Status429TooManyRequests);
            }

            return (false, "Invalid OTP.", null, StatusCodes.Status400BadRequest);
        }

        otpRow.IsVerified = true;
        otpRow.VerifiedOn = DateTime.Now;
        otpRow.AttemptCount = 0;

        var user = await _db.UsersLogins
            .FirstOrDefaultAsync(u => u.MobileNo == mobileNo, cancellationToken);

        var userVerified = false;
        if (user != null)
        {
            user.IsVerified = true;
            userVerified = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, new VerifyOtpResponse
        {
            MobileNo = mobileNo,
            IsVerified = true,
            UserVerified = userVerified
        }, StatusCodes.Status200OK);
    }

    public async Task<(bool Success, string? Error, SendOtpResponse? Data, int StatusCode)> ResendOtpAsync(
        ResendOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var (isValid, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(request.MobileNo);
        if (!isValid)
        {
            return (false, mobileError, null, StatusCodes.Status400BadRequest);
        }

        var otpType = OtpTypeConstants.Normalize(request.OTPType);

        if (otpType == OtpTypeConstants.Registration)
        {
            var existingUser = await _db.UsersLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MobileNo == mobileNo, cancellationToken);

            if (existingUser is { IsVerified: true })
            {
                return (false, "Mobile number already registered.", null, StatusCodes.Status409Conflict);
            }
        }

        // SERIALIZABLE for the same double-tap reason as SendOtpAsync — a concurrent resend must
        // not both read the same SentCount and both increment it (throttle bypass) or both act on
        // a stale "no pending row" view. SendOtpAsync (called below when there's no pending row yet)
        // detects and reuses this ambient transaction instead of nesting. Wrapped in
        // CreateExecutionStrategy().ExecuteAsync since EnableRetryOnFailure is on in Development and
        // a bare BeginTransactionAsync throws under SqlServerRetryingExecutionStrategy — confirmed
        // by testing this locally. The SMS send itself is an external call and deliberately kept
        // out of the transaction (only fired for the "existing pending row" branch here — the
        // "no pending row yet" branch delegates to SendOtpAsync, which sends its own SMS).
        var strategy = _db.Database.CreateExecutionStrategy();
        string? otpCodeToSend = null;

        var result = await strategy.ExecuteAsync(async () =>
        {
            otpCodeToSend = null;
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

            var pending = await _db.UserOTPs
                .Where(o => o.MobileNo == mobileNo
                            && o.OTPType == otpType
                            && !o.IsVerified)
                .OrderByDescending(o => o.CreatedOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (pending == null)
            {
                // No pending row yet — behave like first send. SendOtpAsync sees CurrentTransaction
                // is already set and will not start (or commit/rollback) a second transaction.
                var sendResult = await SendOtpAsync(new SendOtpRequest
                {
                    MobileNo = mobileNo,
                    OTPType = otpType
                }, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return sendResult;
            }

            if (pending.SentCount >= MaxResendCount)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Maximum OTP resend limit reached. Try again later.", null, StatusCodes.Status429TooManyRequests);
            }

            var otpCode = GenerateSixDigitOtp();
            var expiry = DateTime.Now.AddMinutes(OtpExpiryMinutes);

            pending.OTPCode = otpCode;
            pending.ExpiryTime = expiry;
            pending.AttemptCount = 0;
            pending.SentCount += 1;
            pending.VerifiedOn = null;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("OTP resent for {MobileNo} type {OtpType} (SentCount={SentCount})",
                mobileNo, otpType, pending.SentCount);

            otpCodeToSend = otpCode;
            return (true, (string?)null, BuildSendResponse(mobileNo, otpType, expiry, otpCode), StatusCodes.Status200OK);
        });

        if (otpCodeToSend != null)
        {
            await _smsService.SendOtpAsync(mobileNo, otpCodeToSend, cancellationToken);
        }

        return result;
    }

    private SendOtpResponse BuildSendResponse(string mobileNo, string otpType, DateTime expiry, string otpCode) =>
        new()
        {
            MobileNo = mobileNo,
            OTPType = otpType,
            ExpiryTime = expiry,
            OTP = _otpOptions.IncludeInResponse ? otpCode : null
        };

    private static string GenerateSixDigitOtp() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
}

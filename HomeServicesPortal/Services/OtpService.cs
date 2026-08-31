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

        await _smsService.SendOtpAsync(mobileNo, otpCode, cancellationToken);
        _logger.LogInformation("OTP sent for {MobileNo} type {OtpType}", mobileNo, otpType);

        return (true, null, BuildSendResponse(mobileNo, otpType, expiry, otpCode), StatusCodes.Status200OK);
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

        var pending = await _db.UserOTPs
            .Where(o => o.MobileNo == mobileNo
                        && o.OTPType == otpType
                        && !o.IsVerified)
            .OrderByDescending(o => o.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (pending == null)
        {
            // No pending row yet — behave like first send
            return await SendOtpAsync(new SendOtpRequest
            {
                MobileNo = mobileNo,
                OTPType = otpType
            }, cancellationToken);
        }

        if (pending.SentCount >= MaxResendCount)
        {
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
        await _smsService.SendOtpAsync(mobileNo, otpCode, cancellationToken);

        _logger.LogInformation("OTP resent for {MobileNo} type {OtpType} (SentCount={SentCount})",
            mobileNo, otpType, pending.SentCount);

        return (true, null, BuildSendResponse(mobileNo, otpType, expiry, otpCode), StatusCodes.Status200OK);
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

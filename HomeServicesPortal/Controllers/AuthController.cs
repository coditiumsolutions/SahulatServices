using HomeServicesPortal.DTOs;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;

    public AuthController(IAuthService authService, IOtpService otpService)
    {
        _authService = authService;
        _otpService = otpService;
    }

    [HttpPost("register-client")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> RegisterClient(
        [FromBody] RegisterClientRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<RegistrationResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data) = await _authService.RegisterClientAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return BadRequest(ApiResponse<RegistrationResponse>.Fail(error ?? "Registration failed."));
        }

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<RegistrationResponse>.Ok(data, "Client registered successfully. Verify OTP to activate your account."));
    }

    /// <summary>Upgrade an existing client to provider (mobile + password).</summary>
    [HttpPost("register-provider")]
    [ProducesResponseType(typeof(ApiResponse<ProviderUpgradeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderUpgradeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProviderUpgradeResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProviderUpgradeResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProviderUpgradeResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProviderUpgradeResponse>>> RegisterProvider(
        [FromBody] RegisterProviderRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ProviderUpgradeResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data, statusCode) = await _authService.RegisterProviderAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<ProviderUpgradeResponse>.Fail(error ?? "Provider upgrade failed."));
        }

        return Ok(ApiResponse<ProviderUpgradeResponse>.Ok(data, "Account upgraded to provider successfully."));
    }

    [HttpPost("register-staff")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> RegisterStaff(
        [FromBody] RegisterStaffRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<RegistrationResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data) = await _authService.RegisterStaffAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return BadRequest(ApiResponse<RegistrationResponse>.Fail(error ?? "Registration failed."));
        }

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<RegistrationResponse>.Ok(data, "Staff registered successfully."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<LoginResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data, statusCode) = await _authService.LoginAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<LoginResponse>.Fail(error ?? "Login failed."));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(data, "Login successful."));
    }

    /// <summary>Permanently deletes a Client's and/or Provider's account after re-verifying password.</summary>
    [HttpPost("delete-account")]
    [ProducesResponseType(typeof(ApiResponse<DeleteAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeleteAccountResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DeleteAccountResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<DeleteAccountResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DeleteAccountResponse>>> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<DeleteAccountResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data, statusCode) = await _authService.DeleteAccountAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<DeleteAccountResponse>.Fail(error ?? "Account deletion failed."));
        }

        return Ok(ApiResponse<DeleteAccountResponse>.Ok(data, "Account deleted successfully."));
    }

    /// <summary>Send a 6-digit OTP (Development returns OTP in data; Production never does).</summary>
    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SendOtpResponse>>> SendOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<SendOtpResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data, statusCode) = await _otpService.SendOtpAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<SendOtpResponse>.Fail(error ?? "Failed to send OTP."));
        }

        return Ok(ApiResponse<SendOtpResponse>.Ok(data, "OTP sent successfully."));
    }

    /// <summary>Verify the latest unverified OTP for a mobile number.</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<VerifyOtpResponse>>> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VerifyOtpResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data, statusCode) = await _otpService.VerifyOtpAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<VerifyOtpResponse>.Fail(error ?? "OTP verification failed."));
        }

        return Ok(ApiResponse<VerifyOtpResponse>.Ok(data, "OTP verified successfully."));
    }

    /// <summary>Resend OTP — generates a new code, resets attempts, refreshes expiry.</summary>
    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<SendOtpResponse>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<SendOtpResponse>>> ResendOtp(
        [FromBody] ResendOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<SendOtpResponse>.Fail(GetValidationMessage()));
        }

        var (success, error, data, statusCode) = await _otpService.ResendOtpAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<SendOtpResponse>.Fail(error ?? "Failed to resend OTP."));
        }

        return Ok(ApiResponse<SendOtpResponse>.Ok(data, "OTP resent successfully."));
    }

    private string GetValidationMessage() =>
        ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request.";
}

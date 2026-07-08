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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
            ApiResponse<RegistrationResponse>.Ok(data, "Client registered successfully."));
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

    private string GetValidationMessage() =>
        ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid request.";
}

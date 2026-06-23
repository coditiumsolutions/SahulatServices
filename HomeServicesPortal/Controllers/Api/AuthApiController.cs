using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.Api.Auth;
using HomeServicesPortal.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthApiController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Login with email or phone and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<LoginResponseDto>.Fail(message));
        }

        var (success, error, data) = await _authService.LoginAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.Fail(error ?? "Login failed."));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(data, "Login successful."));
    }

    /// <summary>Register a new customer account.</summary>
    [HttpPost("register-customer")]
    [ProducesResponseType(typeof(ApiResponse<RegisterCustomerResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterCustomerResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegisterCustomerResponseDto>>> RegisterCustomer(
        [FromBody] RegisterCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<RegisterCustomerResponseDto>.Fail(message));
        }

        var (success, error, data) = await _authService.RegisterCustomerAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return BadRequest(ApiResponse<RegisterCustomerResponseDto>.Fail(error ?? "Registration failed."));
        }

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<RegisterCustomerResponseDto>.Ok(data, "Customer registered successfully."));
    }

    /// <summary>Register a new service provider account.</summary>
    [HttpPost("register-provider")]
    [ProducesResponseType(typeof(ApiResponse<RegisterProviderResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterProviderResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegisterProviderResponseDto>>> RegisterProvider(
        [FromBody] RegisterProviderRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<RegisterProviderResponseDto>.Fail(message));
        }

        var (success, error, data) = await _authService.RegisterProviderAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return BadRequest(ApiResponse<RegisterProviderResponseDto>.Fail(error ?? "Registration failed."));
        }

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<RegisterProviderResponseDto>.Ok(data, "Provider registered successfully."));
    }
}

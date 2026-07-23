using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

/// <summary>
/// Provider document upload, download, delete, and admin verification APIs.
/// </summary>
[ApiController]
[Route("api/provider")]
[AllowAnonymous]
public class ProviderDocumentsApiController : ControllerBase
{
    private readonly IProviderDocumentsApiService _service;
    private readonly ILogger<ProviderDocumentsApiController> _logger;

    public ProviderDocumentsApiController(
        IProviderDocumentsApiService service,
        ILogger<ProviderDocumentsApiController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Upload profile photo and CNIC images for a provider (multipart/form-data).
    /// Form fields: ProviderUID, ProfilePhoto, CNICFront, CNICBack.
    /// </summary>
    [HttpPost("upload-documents")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProviderDocumentsApiDto>>> UploadDocuments(
        [FromForm] UploadProviderDocumentsRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ProviderDocumentsApiDto>.Fail(message));
        }

        var (success, error, data, statusCode) = await _service.UploadDocumentsAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<ProviderDocumentsApiDto>.Fail(error ?? "Upload failed."));
        }

        return Ok(ApiResponse<ProviderDocumentsApiDto>.Ok(data, "Provider documents uploaded successfully."));
    }

    /// <summary>Get provider document paths and verification status.</summary>
    [HttpGet("{providerUid:int}/documents")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDocumentsApiDto>>> GetDocuments(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data, statusCode) = await _service.GetDocumentsAsync(providerUid, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<ProviderDocumentsApiDto>.Fail(error ?? "Documents not found."));
        }

        return Ok(ApiResponse<ProviderDocumentsApiDto>.Ok(data, "Provider documents fetched successfully."));
    }

    /// <summary>Delete provider document files and database record.</summary>
    [HttpDelete("{providerUid:int}/documents")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDocuments(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error, statusCode) = await _service.DeleteDocumentsAsync(providerUid, cancellationToken);
        if (!success)
        {
            return StatusCode(statusCode, ApiResponse<object>.Fail(error ?? "Delete failed."));
        }

        _logger.LogInformation("Provider documents deleted via API for provider {ProviderUid}", providerUid);
        return Ok(ApiResponse<object>.Ok(new { providerUid }, "Provider documents deleted successfully."));
    }

    /// <summary>Admin verification of provider documents.</summary>
    [HttpPost("verify-documents")]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProviderDocumentsApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderDocumentsApiDto>>> VerifyDocuments(
        [FromBody] VerifyProviderDocumentsRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ProviderDocumentsApiDto>.Fail(message));
        }

        var (success, error, data, statusCode) = await _service.VerifyDocumentsAsync(request, cancellationToken);
        if (!success || data == null)
        {
            return StatusCode(statusCode, ApiResponse<ProviderDocumentsApiDto>.Fail(error ?? "Verification failed."));
        }

        return Ok(ApiResponse<ProviderDocumentsApiDto>.Ok(data, "Provider documents verification updated successfully."));
    }
}

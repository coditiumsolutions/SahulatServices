using HomeServicesPortal.Entities;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

/// <summary>
/// Provider document upload, retrieval, deletion, and admin verification.
/// Files live on disk; SQL stores relative paths only.
/// </summary>
public class ProviderDocumentsApiService : IProviderDocumentsApiService
{
    private readonly IProviderDocumentRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ProviderDocumentsApiService> _logger;

    public ProviderDocumentsApiService(
        IProviderDocumentRepository repository,
        IFileStorageService fileStorage,
        ILogger<ProviderDocumentsApiService> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, ProviderDocumentsApiDto? Data, int StatusCode)> UploadDocumentsAsync(
        UploadProviderDocumentsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProviderUid <= 0)
        {
            return (false, "ProviderUID is required.", null, StatusCodes.Status400BadRequest);
        }

        if (!await _repository.ProviderExistsAsync(request.ProviderUid, cancellationToken))
        {
            _logger.LogWarning("Upload rejected: provider {ProviderUid} not found.", request.ProviderUid);
            return (false, "Provider not found.", null, StatusCodes.Status404NotFound);
        }

        var existing = await _repository.GetByProviderUidAsync(request.ProviderUid, cancellationToken);
        var isFirstSubmission = existing == null;

        var mobileNo = existing?.MobileNo
            ?? await _repository.GetProviderMobileNoAsync(request.ProviderUid, cancellationToken);
        if (string.IsNullOrWhiteSpace(mobileNo))
        {
            return (false, "Provider mobile number is missing.", null, StatusCodes.Status400BadRequest);
        }

        // First-time registration (no documents row yet) must supply all three files.
        // Once a documents row exists, any slot may be omitted to leave it unchanged.
        if (isFirstSubmission)
        {
            if (request.ProfilePhoto == null || request.CnicFront == null || request.CnicBack == null)
            {
                return (false,
                    "ProfilePhoto, CNICFront, and CNICBack are all required for a provider's first document submission.",
                    null,
                    StatusCodes.Status400BadRequest);
            }
        }

        var profilePath = existing?.ProfilePhotoPath;
        if (request.ProfilePhoto != null)
        {
            var profileResult = await _fileStorage.SaveProviderImageAsync(
                request.ProviderUid, request.ProfilePhoto, "profile.jpg", cancellationToken);
            if (!profileResult.Success)
            {
                _logger.LogWarning(
                    "Profile photo validation/upload failed for provider {ProviderUid}: {Error}",
                    request.ProviderUid,
                    profileResult.Error);
                return (false, profileResult.Error, null, profileResult.StatusCode);
            }

            profilePath = profileResult.RelativePath;
        }
        else if (string.IsNullOrEmpty(profilePath))
        {
            return (false,
                "No ProfilePhoto was provided and there is no existing profile photo to keep.",
                null,
                StatusCodes.Status400BadRequest);
        }

        var frontPath = existing?.CnicFrontImagePath;
        if (request.CnicFront != null)
        {
            var frontResult = await _fileStorage.SaveProviderImageAsync(
                request.ProviderUid, request.CnicFront, "cnic_front.jpg", cancellationToken);
            if (!frontResult.Success)
            {
                _logger.LogWarning(
                    "CNIC front validation/upload failed for provider {ProviderUid}: {Error}",
                    request.ProviderUid,
                    frontResult.Error);
                return (false, frontResult.Error, null, frontResult.StatusCode);
            }

            frontPath = frontResult.RelativePath;
        }
        else if (string.IsNullOrEmpty(frontPath))
        {
            return (false,
                "No CNICFront was provided and there is no existing CNIC front image to keep.",
                null,
                StatusCodes.Status400BadRequest);
        }

        var backPath = existing?.CnicBackImagePath;
        if (request.CnicBack != null)
        {
            var backResult = await _fileStorage.SaveProviderImageAsync(
                request.ProviderUid, request.CnicBack, "cnic_back.jpg", cancellationToken);
            if (!backResult.Success)
            {
                _logger.LogWarning(
                    "CNIC back validation/upload failed for provider {ProviderUid}: {Error}",
                    request.ProviderUid,
                    backResult.Error);
                return (false, backResult.Error, null, backResult.StatusCode);
            }

            backPath = backResult.RelativePath;
        }
        else if (string.IsNullOrEmpty(backPath))
        {
            return (false,
                "No CNICBack was provided and there is no existing CNIC back image to keep.",
                null,
                StatusCodes.Status400BadRequest);
        }

        // Added v3.20 — optional on both first submission and edit. Older app builds never send
        // this field, so it's simply omitted and the existing path (if any) is left unchanged.
        var policeVerificationPath = existing?.PoliceVerificationPath;
        if (request.PoliceVerification != null)
        {
            var policeResult = await _fileStorage.SaveProviderImageAsync(
                request.ProviderUid, request.PoliceVerification, "police_verification.jpg", cancellationToken);
            if (!policeResult.Success)
            {
                _logger.LogWarning(
                    "Police verification validation/upload failed for provider {ProviderUid}: {Error}",
                    request.ProviderUid,
                    policeResult.Error);
                return (false, policeResult.Error, null, policeResult.StatusCode);
            }

            policeVerificationPath = policeResult.RelativePath;
        }

        var now = DateTime.Now;

        if (existing == null)
        {
            var document = new ProviderDocument
            {
                ProviderUid = request.ProviderUid,
                MobileNo = mobileNo,
                ProfilePhotoPath = profilePath,
                CnicFrontImagePath = frontPath,
                CnicBackImagePath = backPath,
                PoliceVerificationPath = policeVerificationPath,
                IsVerified = false,
                VerifiedOn = null,
                VerifiedBy = null,
                VerificationRemarks = null,
                CreatedOn = now,
                UpdatedOn = null
            };

            await _repository.AddAsync(document, cancellationToken);
            _logger.LogInformation("Created ProviderDocuments row for provider {ProviderUid}", request.ProviderUid);
        }
        else
        {
            existing.ProfilePhotoPath = profilePath;
            existing.CnicFrontImagePath = frontPath;
            existing.CnicBackImagePath = backPath;
            existing.PoliceVerificationPath = policeVerificationPath;
            existing.MobileNo = mobileNo;

            // Replacing a CNIC image resets Providers.IsVerified until admin re-approves.
            // Profile photo alone does not reset verification.
            if (request.CnicFront != null || request.CnicBack != null)
            {
                existing.VerifiedOn = null;
                existing.VerifiedBy = null;
                existing.VerificationRemarks = null;
                await _repository.SetProviderIsVerifiedAsync(request.ProviderUid, false, cancellationToken);
            }
            existing.UpdatedOn = now;

            await _repository.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation("Updated ProviderDocuments row for provider {ProviderUid}", request.ProviderUid);
        }

        return await GetDocumentsAsync(request.ProviderUid, cancellationToken);
    }

    public async Task<(bool Success, string? Error, ProviderDocumentsApiDto? Data, int StatusCode)> GetDocumentsAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        if (providerUid <= 0)
        {
            return (false, "ProviderUID is required.", null, StatusCodes.Status400BadRequest);
        }

        if (!await _repository.ProviderExistsAsync(providerUid, cancellationToken))
        {
            return (false, "Provider not found.", null, StatusCodes.Status404NotFound);
        }

        var document = await _repository.GetByProviderUidAsync(providerUid, cancellationToken);
        if (document == null)
        {
            return (false, "Provider documents not found.", null, StatusCodes.Status404NotFound);
        }

        var providerVerified = await _repository.GetProviderIsVerifiedAsync(providerUid, cancellationToken) ?? false;
        return (true, null, MapToDto(document, providerVerified), StatusCodes.Status200OK);
    }

    public async Task<(bool Success, string? Error, int StatusCode)> DeleteDocumentsAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        if (providerUid <= 0)
        {
            return (false, "ProviderUID is required.", StatusCodes.Status400BadRequest);
        }

        if (!await _repository.ProviderExistsAsync(providerUid, cancellationToken))
        {
            return (false, "Provider not found.", StatusCodes.Status404NotFound);
        }

        var document = await _repository.GetByProviderUidAsync(providerUid, cancellationToken);
        if (document == null)
        {
            return (false, "Provider documents not found.", StatusCodes.Status404NotFound);
        }

        try
        {
            _fileStorage.DeleteProviderDocumentFiles(providerUid);
            await _repository.DeleteAsync(document, cancellationToken);
            _logger.LogInformation("Deleted provider documents for provider {ProviderUid}", providerUid);
            return (true, null, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete provider documents for provider {ProviderUid}", providerUid);
            return (false, "Failed to delete provider documents.", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<(bool Success, string? Error, ProviderDocumentsApiDto? Data, int StatusCode)> VerifyDocumentsAsync(
        VerifyProviderDocumentsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProviderUid <= 0)
        {
            return (false, "ProviderUID is required.", null, StatusCodes.Status400BadRequest);
        }

        if (request.VerifiedBy <= 0)
        {
            return (false, "VerifiedBy is required.", null, StatusCodes.Status400BadRequest);
        }

        if (!await _repository.ProviderExistsAsync(request.ProviderUid, cancellationToken))
        {
            return (false, "Provider not found.", null, StatusCodes.Status404NotFound);
        }

        var document = await _repository.GetByProviderUidAsync(request.ProviderUid, cancellationToken);
        if (document == null)
        {
            return (false, "Provider documents not found.", null, StatusCodes.Status404NotFound);
        }

        await _repository.SetProviderIsVerifiedAsync(request.ProviderUid, request.IsVerified, cancellationToken);

        // Audit fields stay on the documents row; status is Providers.IsVerified.
        document.VerifiedBy = request.VerifiedBy;
        document.VerificationRemarks = string.IsNullOrWhiteSpace(request.VerificationRemarks)
            ? null
            : request.VerificationRemarks.Trim();
        document.VerifiedOn = DateTime.Now;
        document.UpdatedOn = DateTime.Now;

        await _repository.UpdateAsync(document, cancellationToken);

        _logger.LogInformation(
            "Provider {ProviderUid} verification set to {IsVerified} by {VerifiedBy}",
            request.ProviderUid,
            request.IsVerified,
            request.VerifiedBy);

        return (true, null, MapToDto(document, request.IsVerified), StatusCodes.Status200OK);
    }

    private static ProviderDocumentsApiDto MapToDto(ProviderDocument document, bool providerIsVerified) => new()
    {
        ProviderUid = document.ProviderUid,
        ProfilePhotoPath = document.ProfilePhotoPath,
        CnicFrontImagePath = document.CnicFrontImagePath,
        CnicBackImagePath = document.CnicBackImagePath,
        PoliceVerificationPath = document.PoliceVerificationPath,
        IsVerified = providerIsVerified,
        VerifiedOn = document.VerifiedOn,
        VerifiedBy = document.VerifiedBy,
        VerificationRemarks = document.VerificationRemarks,
        CreatedOn = document.CreatedOn,
        UpdatedOn = document.UpdatedOn
    };
}

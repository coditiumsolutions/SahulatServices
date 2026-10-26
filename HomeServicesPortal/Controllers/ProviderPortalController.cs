using System.ComponentModel.DataAnnotations;
using HomeServicesPortal.Data;
using HomeServicesPortal.Filters;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>Provider-side portal pages: dashboard home, verification pending, availability.</summary>
[Authorize(Roles = UserTypeConstants.Provider)]
public class ProviderPortalController : Controller
{
    private static readonly string[] ActiveStatuses = { "Accepted", "In Progress" };

    private readonly AppDbContext _db;
    private readonly IProviderAvailabilityService _availability;
    private readonly IPaymentService _payments;
    private readonly IProviderDocumentsApiService _documents;
    private readonly ILogger<ProviderPortalController> _logger;

    public ProviderPortalController(
        AppDbContext db,
        IProviderAvailabilityService availability,
        IPaymentService payments,
        IProviderDocumentsApiService documents,
        ILogger<ProviderPortalController> logger)
    {
        _db = db;
        _availability = availability;
        _payments = payments;
        _documents = documents;
        _logger = logger;
    }

    /// <summary>First-time upload and "My Documents". Open to unverified providers (that is the point).</summary>
    [HttpGet("/provider/documents")]
    public async Task<IActionResult> Documents(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        return View("ProviderDocuments", await BuildDocumentsVmAsync(providerUid.Value, cancellationToken));
    }

    [HttpPost("/provider/documents")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]
    public async Task<IActionResult> Documents(
        IFormFile? profilePhoto,
        IFormFile? cnicFront,
        IFormFile? cnicBack,
        IFormFile? policeVerification,
        bool confirmCnicReset,
        CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var current = await BuildDocumentsVmAsync(providerUid.Value, cancellationToken);
        string? problem = null;

        bool Has(IFormFile? f) => f != null && f.Length > 0;
        var changedCnic = Has(cnicFront) || Has(cnicBack);

        if (current.IsOnboarding)
        {
            if (!Has(profilePhoto) || !Has(cnicFront) || !Has(cnicBack))
            {
                problem = "Please add your profile photo and both CNIC images before uploading.";
            }
        }
        else if (!Has(profilePhoto) && !changedCnic && !Has(policeVerification))
        {
            problem = "Replace a photo above to save changes.";
        }
        else if (current.IsVerified && Has(policeVerification))
        {
            problem = "Your Police Verification document is locked after verification. Contact support if it needs to change.";
        }
        else if (current.IsVerified && changedCnic && !confirmCnicReset)
        {
            problem = "Please confirm that replacing your CNIC sends your account back for review.";
        }

        if (problem != null)
        {
            TempData["ProviderError"] = problem;
            return Redirect("/provider/documents");
        }

        var request = new UploadProviderDocumentsRequestDto
        {
            ProviderUid = providerUid.Value,
            ProfilePhoto = Has(profilePhoto) ? profilePhoto : null,
            CnicFront = Has(cnicFront) ? cnicFront : null,
            CnicBack = Has(cnicBack) ? cnicBack : null,
            PoliceVerification = Has(policeVerification) ? policeVerification : null
        };

        var (success, error, data, _) = await _documents.UploadDocumentsAsync(request, cancellationToken);
        if (!success || data == null)
        {
            TempData["ProviderError"] = error ?? (current.IsOnboarding ? "Failed to upload documents" : "Failed to update documents");
            return Redirect("/provider/documents");
        }

        if (data.IsVerified)
        {
            TempData["ProviderSuccess"] = "Your documents were updated successfully.";
            return Redirect("/provider/documents");
        }

        TempData["ProviderSuccess"] = current.IsOnboarding
            ? "Your documents were uploaded successfully."
            : "Your documents were updated successfully.";
        return Redirect("/provider/pending");
    }

    private async Task<ProviderDocumentsVm> BuildDocumentsVmAsync(int providerUid, CancellationToken cancellationToken)
    {
        var (ok, _, data, _) = await _documents.GetDocumentsAsync(providerUid, cancellationToken);
        if (!ok || data == null)
        {
            return new ProviderDocumentsVm();
        }

        // Cache-bust with the last change time so a replaced image shows straight away.
        var version = new DateTimeOffset(DateTime.SpecifyKind(data.UpdatedOn ?? data.CreatedOn, DateTimeKind.Utc))
            .ToUnixTimeMilliseconds();
        string? Url(string? path) =>
            string.IsNullOrWhiteSpace(path) ? null : $"/{path.TrimStart('/')}?v={version}";

        return new ProviderDocumentsVm
        {
            HasDocuments = true,
            IsVerified = data.IsVerified,
            Remarks = string.IsNullOrWhiteSpace(data.VerificationRemarks) ? null : data.VerificationRemarks.Trim(),
            ProfilePhotoUrl = Url(data.ProfilePhotoPath),
            CnicFrontUrl = Url(data.CnicFrontImagePath),
            CnicBackUrl = Url(data.CnicBackImagePath),
            PoliceUrl = Url(data.PoliceVerificationPath)
        };
    }

    [HttpGet("/provider/dashboard")]
    [TypeFilter(typeof(RequireVerifiedProviderFilter))]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var provider = await _db.Providers.AsNoTracking()
            .Where(p => p.Uid == providerUid.Value)
            .Select(p => new { p.FullName, p.AverageRating })
            .FirstAsync(cancellationToken);

        // Today in Pakistan time, as a UTC window for CompletedOn.
        var startUtc = PktTimeHelper.NowPkt.Date.AddHours(-5);
        var endUtc = startUtc.AddDays(1);

        var bookings = _db.ServiceBookings.AsNoTracking().Where(b => b.ProviderUid == providerUid.Value);
        var vm = new ProviderHomeVm
        {
            FullName = provider.FullName,
            AverageRating = provider.AverageRating,
            AvailableRequests = await bookings.CountAsync(b => b.Status == "Pending", cancellationToken),
            ActiveBookings = await bookings.CountAsync(b => ActiveStatuses.Contains(b.Status), cancellationToken),
            CompletedToday = await bookings.CountAsync(
                b => b.CompletedOn != null && b.CompletedOn >= startUtc && b.CompletedOn < endUtc, cancellationToken)
        };

        var (ok, _, status) = await _availability.GetProviderAvailabilityStatusAsync(providerUid.Value, cancellationToken);
        if (ok && status != null)
        {
            vm.IsOnline = status.IsOnline;
            vm.AvailableFrom = status.AvailableFrom ?? vm.AvailableFrom;
            vm.AvailableTo = status.AvailableTo ?? vm.AvailableTo;
        }

        try
        {
            vm.WalletBalance = (await _payments.GetProviderWalletAsync(providerUid.Value, cancellationToken))?.Balance;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load provider wallet balance for dashboard.");
        }

        ViewData["Title"] = "Provider Dashboard";
        return View("ProviderDashboard", vm);
    }

    /// <summary>Online/offline switch. Going online needs a time window (explicit Save in the modal).</summary>
    [HttpPost("/provider/availability")]
    [ValidateAntiForgeryToken]
    [TypeFilter(typeof(RequireVerifiedProviderFilter))]
    public async Task<IActionResult> SetAvailability(
        bool isOnline,
        string? availableFrom,
        string? availableTo,
        string? returnTo,
        CancellationToken cancellationToken)
    {
        var back = returnTo == "profile" ? "/provider/profile" : "/provider/dashboard";
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var request = new SetProviderAvailableStatusRequestDto
        {
            ProviderUid = providerUid.Value,
            IsOnline = isOnline,
            AvailableFrom = isOnline ? availableFrom : null,
            AvailableTo = isOnline ? availableTo : null
        };

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            TempData["ProviderError"] = results.FirstOrDefault()?.ErrorMessage ?? "Failed to update status.";
            return Redirect(back);
        }

        var (success, error, _) = await _availability.SaveProviderAvailabilityStatusAsync(request, cancellationToken);
        if (success)
        {
            TempData["ProviderSuccess"] = isOnline ? "You are now online." : "You are now offline.";
        }
        else
        {
            TempData["ProviderError"] = error ?? "Failed to update status.";
        }

        return Redirect(back);
    }

    /// <summary>Shown to providers whose documents have not been verified yet.</summary>
    [HttpGet("/provider/pending")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var provider = await _db.Providers.AsNoTracking()
            .Where(p => p.Uid == providerUid.Value)
            .Select(p => new { p.FullName, p.IsVerified })
            .FirstAsync(cancellationToken);
        if (provider.IsVerified)
        {
            return Redirect("/provider/dashboard");
        }

        var document = await _db.ProviderDocuments.AsNoTracking()
            .Where(d => d.ProviderUid == providerUid.Value)
            .Select(d => d.VerificationRemarks)
            .FirstOrDefaultAsync(cancellationToken);
        var hasDocuments = await _db.ProviderDocuments.AsNoTracking()
            .AnyAsync(d => d.ProviderUid == providerUid.Value, cancellationToken);

        ViewData["Title"] = "Pending Verification";
        return View("ProviderPending", new ProviderPendingVm
        {
            FullName = provider.FullName,
            Remarks = string.IsNullOrWhiteSpace(document) ? null : document.Trim(),
            HasDocuments = hasDocuments
        });
    }
}

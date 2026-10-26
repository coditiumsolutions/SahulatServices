using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Filters;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>Provider profile page, profile edit, categories and services, and account deletion.</summary>
[Authorize(Roles = UserTypeConstants.Provider)]
[TypeFilter(typeof(RequireVerifiedProviderFilter))]
public class ProviderProfileController : Controller
{
    private static readonly string[] Genders = { "Male", "Female", "Other" };

    /// <summary>Account deletion from the website is switched off for now; flip to true to re-enable (and restore the profile page button).</summary>
    private static readonly bool DeleteAccountEnabled = false;

    private readonly AppDbContext _db;
    private readonly IProviderDetailService _details;
    private readonly IProviderDocumentsApiService _documents;
    private readonly IProviderAvailabilityService _availability;
    private readonly IProviderCategoryService _categories;
    private readonly IProviderServiceTitleService _titles;
    private readonly IProviderZoneService _zones;
    private readonly IConfigurationEntryService _configurations;
    private readonly IAuthService _authService;
    private readonly ILogger<ProviderProfileController> _logger;

    public ProviderProfileController(
        AppDbContext db,
        IProviderDetailService details,
        IProviderDocumentsApiService documents,
        IProviderAvailabilityService availability,
        IProviderCategoryService categories,
        IProviderServiceTitleService titles,
        IProviderZoneService zones,
        IConfigurationEntryService configurations,
        IAuthService authService,
        ILogger<ProviderProfileController> logger)
    {
        _db = db;
        _details = details;
        _documents = documents;
        _availability = availability;
        _categories = categories;
        _titles = titles;
        _zones = zones;
        _configurations = configurations;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("/provider/profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var (ok, _, detail) = await _details.GetProviderDetailAsync(providerUid.Value, cancellationToken);
        if (!ok || detail == null)
        {
            return Redirect("/provider/dashboard");
        }

        var vm = new ProviderProfileVm
        {
            ProviderUid = detail.Uid,
            FullName = detail.FullName,
            MobileNo = detail.MobileNo,
            Cnic = detail.Cnic,
            Gender = detail.Gender,
            ExperienceYears = detail.ExperienceYears ?? 0,
            City = detail.City,
            Description = detail.Description,
            CategoryName = detail.CategoryName,
            IsVerified = detail.IsVerified,
            AverageRating = detail.AverageRating,
            TotalReviews = detail.TotalReviews,
            JobsCompleted = detail.TotalJobsCompleted,
            CreatedOn = detail.CreatedOn
        };

        var (docOk, _, docs, _) = await _documents.GetDocumentsAsync(providerUid.Value, cancellationToken);
        if (docOk && docs != null)
        {
            var version = new DateTimeOffset(DateTime.SpecifyKind(docs.UpdatedOn ?? docs.CreatedOn, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
            vm.PhotoUrl = string.IsNullOrWhiteSpace(docs.ProfilePhotoPath) ? null : $"/{docs.ProfilePhotoPath.TrimStart('/')}?v={version}";
            vm.VerificationRemarks = string.IsNullOrWhiteSpace(docs.VerificationRemarks) ? null : docs.VerificationRemarks.Trim();
        }

        var (availOk, _, status) = await _availability.GetProviderAvailabilityStatusAsync(providerUid.Value, cancellationToken);
        if (availOk && status != null)
        {
            vm.IsOnline = status.IsOnline;
            vm.AvailableFrom = status.AvailableFrom ?? vm.AvailableFrom;
            vm.AvailableTo = status.AvailableTo ?? vm.AvailableTo;
        }

        var categories = await _categories.GetCategoriesAsync(providerUid.Value, cancellationToken);
        vm.Categories = categories.Select(c => c.CategoryName).ToList();
        vm.PrimaryCategory = categories.FirstOrDefault(c => c.IsPrimary)?.CategoryName;
        vm.Zones = await _zones.GetZonesAsync(providerUid.Value, cancellationToken);
        vm.Titles = (await _titles.GetServiceTitlesAsync(providerUid.Value, cancellationToken))
            .GroupBy(t => t.CategoryName, t => t.Title)
            .ToList();

        ViewData["Title"] = "Provider Profile";
        return View("ProviderProfile", vm);
    }

    [HttpGet("/provider/profile/edit")]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var (ok, _, detail) = await _details.GetProviderDetailAsync(providerUid.Value, cancellationToken);
        if (!ok || detail == null)
        {
            return Redirect("/provider/profile");
        }

        var vm = new ProviderProfileEditVm
        {
            FullName = detail.FullName,
            Gender = detail.Gender,
            Cnic = detail.Cnic,
            ExperienceYears = detail.ExperienceYears ?? 0,
            City = detail.City,
            Description = detail.Description,
            Zones = await _zones.GetZonesAsync(providerUid.Value, cancellationToken)
        };
        await FillEditOptionsAsync(vm, cancellationToken);
        vm.MobileNo = detail.MobileNo;
        ViewData["Title"] = "Edit Profile";
        return View("ProviderProfileEdit", vm);
    }

    [HttpPost("/provider/profile/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProviderProfileEditVm model, CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        await FillEditOptionsAsync(model, cancellationToken);
        model.MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty;
        ViewData["Title"] = "Edit Profile";

        var gender = Genders.FirstOrDefault(g => g.Equals(model.Gender?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (gender == null && ModelState.GetFieldValidationState(nameof(model.Gender)) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
        {
            ModelState.AddModelError(nameof(model.Gender), "Please select a gender.");
        }

        var city = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        if (city != null && !model.Cities.Contains(city, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.City), "Select a city from the list.");
        }

        var zones = model.Zones.Where(z => !string.IsNullOrWhiteSpace(z)).Select(z => z.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (zones.Count > ProviderProfileEditVm.MaxZones)
        {
            ModelState.AddModelError(nameof(model.Zones), $"You can select up to {ProviderProfileEditVm.MaxZones} zones.");
        }

        if (!ModelState.IsValid)
        {
            return View("ProviderProfileEdit", model);
        }

        // The detail endpoint needs the current primary category; this page does not change it.
        var (ok, _, current) = await _details.GetProviderDetailAsync(providerUid.Value, cancellationToken);
        if (!ok || current == null)
        {
            return Redirect("/provider/profile");
        }

        var request = new UpdateProviderDetailRequestDto
        {
            ProviderUid = providerUid.Value,
            FullName = model.FullName.Trim(),
            CNIC = model.Cnic.Trim(),
            Gender = gender,
            ExperienceYears = model.ExperienceYears ?? 0,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            City = city,
            CategoryId = current.CategoryId
        };

        var (success, error, _) = await _details.UpdateProviderDetailAsync(request, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update profile");
            return View("ProviderProfileEdit", model);
        }

        var (zonesOk, zonesError) = await _zones.UpdateZonesAsync(providerUid.Value, zones, cancellationToken);
        if (!zonesOk)
        {
            ModelState.AddModelError(nameof(model.Zones), zonesError ?? "Failed to update zones.");
            return View("ProviderProfileEdit", model);
        }

        await HttpContext.RefreshNameClaimAsync(request.FullName);
        TempData["ProfileSuccess"] = "Profile updated.";
        return Redirect("/provider/profile");
    }

    [HttpGet("/provider/categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        return View("ProviderCategories", await BuildCategoriesVmAsync(providerUid.Value, null, null, cancellationToken));
    }

    [HttpPost("/provider/categories")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Categories(
        List<int> categoryUids,
        int? primaryCategoryUid,
        List<int> serviceTitleUids,
        CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var chosen = categoryUids.Where(id => id > 0).Distinct().ToList();
        string? problem = null;
        if (chosen.Count == 0)
        {
            problem = "Select at least one category.";
        }
        else if (chosen.Count > ProviderCategoriesVm.MaxCategories)
        {
            problem = $"A provider can have at most {ProviderCategoriesVm.MaxCategories} categories.";
        }
        else if (chosen.Count == 1)
        {
            primaryCategoryUid = chosen[0];
        }
        else if (primaryCategoryUid == null || !chosen.Contains(primaryCategoryUid.Value))
        {
            problem = "The primary category must be one of the selected categories.";
        }

        if (problem == null)
        {
            var (success, error) = await _categories.SyncCategoriesAsync(providerUid.Value, chosen, primaryCategoryUid!.Value, cancellationToken);
            if (!success)
            {
                problem = error ?? "Failed to update categories";
            }
        }

        if (problem == null)
        {
            // Like the app: titles that belong to a category the provider no longer has are dropped.
            var allowed = await _db.ServiceTitles.AsNoTracking()
                .Where(t => t.IsActive && chosen.Contains(t.CategoryUid))
                .Select(t => t.Uid)
                .ToListAsync(cancellationToken);
            var keep = serviceTitleUids.Distinct().Where(allowed.Contains).ToList();
            var (success, error) = await _titles.SyncServiceTitlesAsync(providerUid.Value, keep, cancellationToken);
            if (!success)
            {
                problem = error ?? "Failed to update services";
            }
        }

        if (problem != null)
        {
            TempData["ProfileError"] = problem;
            return View("ProviderCategories", await BuildCategoriesVmAsync(providerUid.Value, chosen, serviceTitleUids, cancellationToken, primaryCategoryUid));
        }

        TempData["ProfileSuccess"] = "Categories and services updated.";
        return Redirect("/provider/profile");
    }

    [HttpPost("/provider/profile/delete-account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? password, CancellationToken cancellationToken)
    {
        if (!DeleteAccountEnabled)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            TempData["ProfileError"] = "Password is required.";
            return Redirect("/provider/profile");
        }

        var request = new DeleteAccountRequest
        {
            MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty,
            Password = password
        };

        var (success, error, _, _) = await _authService.DeleteAccountAsync(request, cancellationToken);
        if (!success)
        {
            TempData["ProfileError"] = error ?? "Failed to delete account";
            return Redirect("/provider/profile");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Provider deleted their account from the website.");
        TempData["SignupSuccess"] = "Account deleted successfully.";
        return Redirect("/login");
    }

    private async Task FillEditOptionsAsync(ProviderProfileEditVm model, CancellationToken cancellationToken)
    {
        model.Cities = (await _configurations.GetValuesByKeyAsync("Cities", cancellationToken)).ToList();
        model.ZoneOptions = await _zones.GetZoneOptionsAsync(cancellationToken);
    }

    private async Task<ProviderCategoriesVm> BuildCategoriesVmAsync(
        int providerUid,
        List<int>? postedCategories,
        List<int>? postedTitles,
        CancellationToken cancellationToken,
        int? postedPrimary = null)
    {
        var current = await _categories.GetCategoriesAsync(providerUid, cancellationToken);
        var selectedCategories = postedCategories ?? current.Select(c => c.CategoryUid).ToList();
        var primary = postedPrimary ?? current.FirstOrDefault(c => c.IsPrimary)?.CategoryUid;
        var selectedTitles = postedTitles ?? await _titles.GetServiceTitleUidsAsync(providerUid, cancellationToken);

        var categoryRows = await _db.ServiceCategories.AsNoTracking()
            .Where(c => c.IsActive && c.Service.IsActive)
            .OrderBy(c => c.Service.DisplayOrder).ThenBy(c => c.Service.ServiceName).ThenBy(c => c.CategoryName)
            .Select(c => new { c.Uid, c.CategoryName, c.Service.ServiceName })
            .ToListAsync(cancellationToken);

        var titleRows = await _db.ServiceTitles.AsNoTracking()
            .Where(t => t.IsActive && t.Category.IsActive)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Title)
            .Select(t => new ProviderTitleOptionVm { Uid = t.Uid, Title = t.Title, CategoryUid = t.CategoryUid })
            .ToListAsync(cancellationToken);
        foreach (var t in titleRows)
        {
            t.Selected = selectedTitles.Contains(t.Uid);
        }

        return new ProviderCategoriesVm
        {
            Services = categoryRows
                .Select(c => new { c.ServiceName, Option = new ProviderCategoryOptionVm
                {
                    Uid = c.Uid,
                    Name = c.CategoryName,
                    Selected = selectedCategories.Contains(c.Uid),
                    Primary = primary == c.Uid
                } })
                .GroupBy(x => x.ServiceName, x => x.Option)
                .ToList(),
            Titles = titleRows,
            CategoryNames = categoryRows.ToDictionary(c => c.Uid, c => c.CategoryName)
        };
    }
}

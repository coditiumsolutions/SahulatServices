using System.Security.Claims;
using System.Text.Json;
using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>Post-login dashboards and portal sections for Client and Provider cookie sessions.</summary>
public class PublicDashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICustomerServiceRequestService _requestService;
    private readonly ILogger<PublicDashboardController> _logger;

    public PublicDashboardController(
        AppDbContext db,
        ICustomerServiceRequestService requestService,
        ILogger<PublicDashboardController> logger)
    {
        _db = db;
        _requestService = requestService;
        _logger = logger;
    }

    [HttpGet("/client/dashboard")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientDashboard(CancellationToken cancellationToken)
    {
        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.ServiceName)
            .Select(s => new ClientServiceCardVm { Uid = s.Uid, ServiceName = s.ServiceName, Description = s.Description })
            .ToListAsync(cancellationToken);
        foreach (var card in services)
        {
            card.IconClass = ResolveServiceIcon(card.ServiceName);
        }

        var categories = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive && c.Service.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new ClientSearchCategoryVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                ServiceUid = c.ServiceUid,
                ServiceName = c.Service.ServiceName
            })
            .ToListAsync(cancellationToken);

        return View("ClientDashboard", new ClientDashboardVm
        {
            FullName = User.Identity?.Name ?? "User",
            Services = services,
            Categories = categories
        });
    }

    // The provider dashboard itself lives in ProviderPortalController.

    // Old URLs, kept so existing bookmarks keep working.
    [HttpGet("/client-dashboard")]
    public IActionResult LegacyClientDashboard() => RedirectPermanent("/client/dashboard");

    [HttpGet("/provider-dashboard")]
    public IActionResult LegacyProviderDashboard() => RedirectPermanent("/provider/dashboard");

    [HttpGet("/client/services")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientServices(CancellationToken cancellationToken)
    {
        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.ServiceName)
            .Select(s => new ClientServiceCardVm
            {
                Uid = s.Uid,
                ServiceName = s.ServiceName,
                Description = s.Description
            })
            .ToListAsync(cancellationToken);

        foreach (var card in services)
        {
            card.IconClass = ResolveServiceIcon(card.ServiceName);
        }

        return View(new ClientServicesVm
        {
            FullName = User.Identity?.Name ?? "User",
            Services = services
        });
    }

    [HttpGet("/client/services/{serviceUid:int}")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientServiceCategories(int serviceUid, CancellationToken cancellationToken)
    {
        var service = await _db.Services
            .AsNoTracking()
            .Where(s => s.Uid == serviceUid && s.IsActive)
            .Select(s => new { s.Uid, s.ServiceName, s.Description })
            .FirstOrDefaultAsync(cancellationToken);

        if (service == null)
        {
            return NotFound();
        }

        var categories = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.ServiceUid == serviceUid && c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new ClientCategoryCardVm
            {
                Uid = c.Uid,
                CategoryName = c.CategoryName,
                Description = c.Description
            })
            .ToListAsync(cancellationToken);

        return View(new ClientServiceCategoriesVm
        {
            ServiceUid = service.Uid,
            ServiceName = service.ServiceName,
            ServiceDescription = service.Description,
            Categories = categories
        });
    }

    [HttpGet("/client/services/{serviceUid:int}/categories/{categoryUid:int}/request")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientServiceRequestForm(
        int serviceUid,
        int categoryUid,
        CancellationToken cancellationToken)
    {
        var form = await BuildRequestFormAsync(serviceUid, categoryUid, cancellationToken);
        if (form == null)
        {
            return NotFound();
        }

        return View(form);
    }

    [HttpPost("/client/services/{serviceUid:int}/categories/{categoryUid:int}/request")]
    [Authorize(Roles = UserTypeConstants.Client)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClientServiceRequestForm(
        int serviceUid,
        int categoryUid,
        ClientServiceRequestFormVm model,
        CancellationToken cancellationToken)
    {
        model.ServiceUid = serviceUid;
        model.CategoryUid = categoryUid;

        var form = await BuildRequestFormAsync(serviceUid, categoryUid, cancellationToken, model);
        if (form == null)
        {
            return NotFound();
        }

        if (!form.HasAddresses)
        {
            ModelState.AddModelError(nameof(model.ClientAddressUid), "Add a service address to your profile before submitting a request.");
        }

        var isOther = model.IsOtherTitle;
        if (model.ServiceTitleUid is null or 0)
        {
            ModelState.AddModelError(nameof(model.ServiceTitleUid), "Required");
        }

        if (isOther)
        {
            if (string.IsNullOrWhiteSpace(model.OtherServiceTitle))
            {
                ModelState.AddModelError(nameof(model.OtherServiceTitle), "Required");
            }

            if (string.IsNullOrWhiteSpace(model.ServiceDescription))
            {
                ModelState.AddModelError(nameof(model.ServiceDescription), "Please describe the job so staff can assign a provider");
            }
        }

        var accountName = User.Identity?.Name ?? string.Empty;
        var accountMobile = await ResolveMobileNoAsync(cancellationToken);
        string contactPerson;
        string contactNo;
        if (model.SameAsAccount)
        {
            contactPerson = accountName;
            contactNo = accountMobile;
        }
        else
        {
            contactPerson = model.ContactPerson?.Trim() ?? string.Empty;
            if (contactPerson.Length == 0)
            {
                ModelState.AddModelError(nameof(model.ContactPerson), "Required");
            }

            var (contactOk, normalized, _) = MobileNumberHelper.ValidateAndNormalize(model.ContactNo);
            contactNo = normalized;
            if (string.IsNullOrWhiteSpace(model.ContactNo))
            {
                ModelState.AddModelError(nameof(model.ContactNo), "Required");
            }
            else if (!contactOk)
            {
                ModelState.AddModelError(nameof(model.ContactNo), "Enter a valid 11-digit mobile number");
            }
        }

        if (model.PreferredServiceDate is { } preferredDate)
        {
            var today = DateOnly.FromDateTime(PktTimeHelper.NowPkt);
            if (preferredDate < today || preferredDate > today.AddDays(365))
            {
                ModelState.AddModelError(nameof(model.PreferredServiceDate), "Pick a date from today up to one year ahead.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.PreferredServiceTime)
            && !TimeOnly.TryParseExact(model.PreferredServiceTime.Trim(), "HH:mm", out _))
        {
            ModelState.AddModelError(nameof(model.PreferredServiceTime), "Enter a valid time.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        int? titleUid = null;
        string titleText;
        if (isOther)
        {
            titleText = model.OtherServiceTitle!.Trim();
        }
        else
        {
            var title = await _db.ServiceTitles
                .AsNoTracking()
                .Where(t => t.Uid == model.ServiceTitleUid && t.IsActive && t.CategoryUid == categoryUid)
                .Select(t => new { t.Uid, t.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (title == null)
            {
                ModelState.AddModelError(nameof(model.ServiceTitleUid), "Invalid or inactive service title.");
                return View(form);
            }

            titleUid = title.Uid;
            titleText = title.Title;
        }

        var createDto = new CreateCustomerServiceRequestDto
        {
            ClientUid = clientUid.Value,
            CategoryUid = categoryUid,
            ClientAddressUid = model.ClientAddressUid.GetValueOrDefault(),
            ServiceTitle = titleText,
            ServiceTitleUid = titleUid,
            ServiceDescription = model.ServiceDescription?.Trim(),
            PreferredServiceDate = model.PreferredServiceDate,
            PreferredServiceTime = string.IsNullOrWhiteSpace(model.PreferredServiceTime) ? null : model.PreferredServiceTime.Trim(),
            IsUrgent = model.IsUrgent,
            ContactPerson = contactPerson,
            ContactNo = contactNo,
            Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks.Trim()
        };

        var (success, error, data) = await _requestService.CreateRequestAsync(createDto, cancellationToken);
        if (!success || data == null)
        {
            _logger.LogWarning("Client web request create failed: {Error}", error);
            ModelState.AddModelError(string.Empty, error ?? "Failed to submit request");
            return View(form);
        }

        TempData["RequestSuccess"] = $"Request #{data.Uid} submitted successfully.";
        return Redirect("/client/requests");
    }

    [HttpGet("/client/requests")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientRequests(string? tab, CancellationToken cancellationToken)
    {
        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var all = (await _requestService.GetRequestsAsync(clientUid.Value, cancellationToken))
            .Select(ClientRequestCardVm.From)
            .OrderByDescending(r => r.CreatedOn)
            .ToList();

        // Same split as the app: Completed = "Completed", Cancelled = no progress stage, Active = the rest.
        static string TabOf(ClientRequestCardVm r) => r.ProgressStatus switch
        {
            null => ClientRequestsVm.TabCancelled,
            "Completed" => ClientRequestsVm.TabCompleted,
            _ => ClientRequestsVm.TabActive
        };

        var selected = tab?.ToLowerInvariant() switch
        {
            ClientRequestsVm.TabCompleted => ClientRequestsVm.TabCompleted,
            ClientRequestsVm.TabCancelled => ClientRequestsVm.TabCancelled,
            _ => ClientRequestsVm.TabActive
        };

        return View(new ClientRequestsVm
        {
            FullName = User.Identity?.Name ?? "User",
            Tab = selected,
            ActiveCount = all.Count(r => TabOf(r) == ClientRequestsVm.TabActive),
            CompletedCount = all.Count(r => TabOf(r) == ClientRequestsVm.TabCompleted),
            CancelledCount = all.Count(r => TabOf(r) == ClientRequestsVm.TabCancelled),
            Requests = all.Where(r => TabOf(r) == selected).ToList()
        });
    }

    [HttpGet("/client/requests/{requestUid:int}")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientRequestDetail(int requestUid, CancellationToken cancellationToken)
    {
        var request = await GetOwnedRequestAsync(requestUid, cancellationToken);
        if (request == null)
        {
            return NotFound();
        }

        return View(ClientRequestCardVm.From(request));
    }

    [HttpPost("/client/requests/{requestUid:int}/cancel")]
    [Authorize(Roles = UserTypeConstants.Client)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClientRequestCancel(
        int requestUid,
        CancelRequestForm form,
        CancellationToken cancellationToken)
    {
        var back = form.ReturnTo == "detail" ? $"/client/requests/{requestUid}" : "/client/requests";

        var request = await GetOwnedRequestAsync(requestUid, cancellationToken);
        if (request == null)
        {
            return NotFound();
        }

        var card = ClientRequestCardVm.From(request);
        if (!card.CanCancel)
        {
            TempData["RequestError"] = "This request can no longer be cancelled.";
            return Redirect(back);
        }

        var reason = string.Equals(form.Reason, "Other", StringComparison.OrdinalIgnoreCase)
            ? form.OtherReason?.Trim()
            : form.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["RequestError"] = "Cancel reason is required when cancelling a service request.";
            return Redirect(back);
        }

        var update = new UpdateCustomerServiceRequestDto
        {
            RequestUid = request.Uid,
            CategoryUid = request.CategoryUid,
            ClientAddressUid = request.ClientAddressUid,
            ServiceTitle = request.ServiceTitle,
            ServiceDescription = request.ServiceDescription,
            PreferredServiceDate = request.PreferredServiceDate,
            PreferredServiceTime = request.PreferredServiceTime,
            IsUrgent = request.IsUrgent,
            ContactPerson = request.ContactPerson,
            ContactNo = request.ContactNo,
            Remarks = request.Remarks,
            Status = "Cancelled",
            CancelReason = reason.Length > 500 ? reason[..500] : reason
        };

        var (success, error, _) = await _requestService.UpdateRequestAsync(update, cancellationToken);
        if (success)
        {
            TempData["RequestSuccess"] = "Request cancelled";
        }
        else
        {
            _logger.LogWarning("Client web request cancel failed for {RequestUid}: {Error}", requestUid, error);
            TempData["RequestError"] = error ?? "Failed to cancel request";
        }

        return Redirect(back);
    }

    /// <summary>Loads a request only when it belongs to the signed-in client (the API itself does not check).</summary>
    private async Task<CustomerServiceRequestApiDto?> GetOwnedRequestAsync(int requestUid, CancellationToken cancellationToken)
    {
        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return null;
        }

        var (success, _, data) = await _requestService.GetRequestByIdAsync(requestUid, cancellationToken);
        return success && data != null && data.ClientUid == clientUid.Value ? data : null;
    }

    [HttpGet("/provider/bookings")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    [TypeFilter(typeof(HomeServicesPortal.Filters.RequireVerifiedProviderFilter))]
    public IActionResult ProviderBookings() => PortalSection("Provider", "Bookings", "Your accepted and active jobs will appear here.");

    [HttpGet("/provider/wallet")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    [TypeFilter(typeof(HomeServicesPortal.Filters.RequireVerifiedProviderFilter))]
    public IActionResult ProviderWallet() => PortalSection("Provider", "Wallet", "Your earnings and payouts will appear here.");

    private async Task<ClientServiceRequestFormVm?> BuildRequestFormAsync(
        int serviceUid,
        int categoryUid,
        CancellationToken cancellationToken,
        ClientServiceRequestFormVm? posted = null)
    {
        var category = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.Uid == categoryUid && c.IsActive && c.ServiceUid == serviceUid)
            .Select(c => new
            {
                c.Uid,
                c.CategoryName,
                ServiceName = c.Service.ServiceName,
                ServiceUid = c.ServiceUid
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            return null;
        }

        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return null;
        }

        var addresses = await _db.ClientAddresses
            .AsNoTracking()
            .Where(a => a.ClientUid == clientUid.Value)
            .OrderBy(a => a.AddressTitle)
            .Select(a => new SelectListItem
            {
                Value = a.Uid.ToString(),
                Text = a.AddressTitle + " — " + a.FullAddress + ", " + a.Area + ", " + a.City,
                Selected = posted != null && posted.ClientAddressUid == a.Uid
            })
            .ToListAsync(cancellationToken);

        var titleRows = await _db.ServiceTitles
            .AsNoTracking()
            .Where(t => t.CategoryUid == categoryUid && t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Title)
            .Select(t => new
            {
                t.Uid,
                t.Title,
                t.BasePrice,
                t.EstimateText
            })
            .ToListAsync(cancellationToken);

        var titles = titleRows
            .Select(t => new SelectListItem
            {
                Value = t.Uid.ToString(),
                Text = t.Title,
                Selected = posted != null && posted.ServiceTitleUid == t.Uid
            })
            .ToList();

        var budgetMap = titleRows.ToDictionary(
            t => t.Uid.ToString(),
            t => new
            {
                title = t.Title,
                basePrice = t.BasePrice,
                estimateText = t.EstimateText
            });

        var form = posted ?? new ClientServiceRequestFormVm();
        var todayPkt = DateOnly.FromDateTime(PktTimeHelper.NowPkt);
        form.MinDate = todayPkt.ToString("yyyy-MM-dd");
        form.MaxDate = todayPkt.AddDays(365).ToString("yyyy-MM-dd");
        form.AccountName = User.Identity?.Name ?? string.Empty;
        form.AccountMobile = await ResolveMobileNoAsync(cancellationToken);
        form.ServiceUid = category.ServiceUid;
        form.ServiceName = category.ServiceName;
        form.CategoryUid = category.Uid;
        form.CategoryName = category.CategoryName;
        form.Addresses = addresses;
        form.Titles = titles;
        form.TitlesBudgetJson = JsonSerializer.Serialize(
            budgetMap,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        form.EstimatedBudgetDisplay = ResolveBudgetDisplay(
            titleRows.FirstOrDefault(t => t.Uid == form.ServiceTitleUid)?.EstimateText,
            titleRows.FirstOrDefault(t => t.Uid == form.ServiceTitleUid)?.BasePrice);

        return form;
    }

    private static string ResolveBudgetDisplay(string? estimateText, decimal? basePrice)
    {
        if (!string.IsNullOrWhiteSpace(estimateText))
        {
            return estimateText.Trim();
        }

        if (basePrice.HasValue)
        {
            return "Rs. " + basePrice.Value.ToString("N0");
        }

        return "Select a service title";
    }

    private async Task<string> ResolveMobileNoAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.MobilePhone);
        if (!string.IsNullOrWhiteSpace(claim))
        {
            return claim;
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return string.Empty;
        }

        return await _db.UsersLogins.AsNoTracking()
            .Where(u => u.Uid == userId)
            .Select(u => u.MobileNo)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    private async Task<int?> ResolveClientUidAsync(CancellationToken cancellationToken)
    {
        if (int.TryParse(User.FindFirstValue("ProfileId"), out var profileId) && profileId > 0)
        {
            var exists = await _db.Clients.AsNoTracking()
                .AnyAsync(c => c.Uid == profileId, cancellationToken);
            if (exists)
            {
                return profileId;
            }
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        return await _db.Clients.AsNoTracking()
            .Where(c => c.UserUid == userId)
            .Select(c => (int?)c.Uid)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IActionResult PortalSection(string role, string title, string blurb)
    {
        ViewData["PortalRole"] = role;
        ViewData["SectionTitle"] = title;
        ViewData["SectionBlurb"] = blurb;
        ViewData["Title"] = title;
        return View("PortalSection", new PublicDashboardVm
        {
            FullName = User.Identity?.Name ?? "User",
            UserType = role,
            MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty,
            ProfileId = int.TryParse(User.FindFirstValue("ProfileId"), out var pid) ? pid : 0
        });
    }

    private static string ResolveServiceIcon(string serviceName)
    {
        var name = serviceName.ToLowerInvariant();
        if (name.Contains("electr")) return "fa-solid fa-bolt";
        if (name.Contains("plumb")) return "fa-solid fa-faucet-drip";
        if (name.Contains("clean")) return "fa-solid fa-broom";
        if (name.Contains("ac") || name.Contains("air")) return "fa-solid fa-fan";
        if (name.Contains("carp")) return "fa-solid fa-hammer";
        if (name.Contains("paint")) return "fa-solid fa-paint-roller";
        if (name.Contains("tutor") || name.Contains("educ")) return "fa-solid fa-book-open";
        if (name.Contains("maint")) return "fa-solid fa-wrench";
        if (name.Contains("legal") || name.Contains("propert")) return "fa-solid fa-scale-balanced";
        if (name.Contains("special")) return "fa-solid fa-star";
        return "fa-solid fa-screwdriver-wrench";
    }
}

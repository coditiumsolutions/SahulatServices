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

    [HttpGet("/client-dashboard")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public Task<IActionResult> ClientDashboard(CancellationToken cancellationToken) =>
        DashboardAsync("Client", cancellationToken);

    [HttpGet("/provider-dashboard")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    public Task<IActionResult> ProviderDashboard(CancellationToken cancellationToken) =>
        DashboardAsync("Provider", cancellationToken);

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

        if (!form.HasTitles)
        {
            ModelState.AddModelError(nameof(model.ServiceTitleUid), "No service titles are available for this category yet.");
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

        var mobileNo = User.FindFirstValue(ClaimTypes.MobilePhone)
            ?? await _db.UsersLogins.AsNoTracking()
                .Where(u => u.Uid == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
                .Select(u => u.MobileNo)
                .FirstAsync(cancellationToken);

        var createDto = new CreateCustomerServiceRequestDto
        {
            ClientUid = clientUid.Value,
            CategoryUid = categoryUid,
            ClientAddressUid = model.ClientAddressUid,
            ServiceTitle = title.Title,
            ServiceTitleUid = title.Uid,
            ServiceDescription = model.ServiceDescription,
            IsUrgent = model.IsUrgent,
            ContactPerson = User.Identity?.Name,
            ContactNo = mobileNo
        };

        var (success, error, data) = await _requestService.CreateRequestAsync(createDto, cancellationToken);
        if (!success || data == null)
        {
            _logger.LogWarning("Client web request create failed: {Error}", error);
            ModelState.AddModelError(string.Empty, error ?? "Could not create the service request.");
            return View(form);
        }

        TempData["RequestSuccess"] = $"Request #{data.Uid} submitted successfully.";
        return Redirect("/client/requests");
    }

    [HttpGet("/client/requests")]
    [Authorize(Roles = UserTypeConstants.Client)]
    public async Task<IActionResult> ClientRequests(CancellationToken cancellationToken)
    {
        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var requests = await _requestService.GetRequestsAsync(clientUid.Value, cancellationToken);
        var cards = requests.Select(r => new ClientRequestCardVm
        {
            Uid = r.Uid,
            ServiceTitle = r.ServiceTitle,
            CategoryName = r.CategoryName,
            AddressTitle = r.AddressTitle,
            ServiceDescription = r.ServiceDescription,
            EstimatedBudget = r.EstimatedBudget,
            IsUrgent = r.IsUrgent,
            Status = r.Status,
            ProgressStatus = r.ProgressStatus,
            CreatedOn = r.CreatedOn,
            ProviderName = r.ProviderName
        }).ToList();

        return View(new ClientRequestsVm
        {
            FullName = User.Identity?.Name ?? "User",
            Requests = cards
        });
    }

    [HttpGet("/provider/requests")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    public IActionResult ProviderRequests() => PortalSection("Provider", "Requests", "Incoming job requests will appear here.");

    [HttpGet("/provider/bookings")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    public IActionResult ProviderBookings() => PortalSection("Provider", "Bookings", "Your accepted and active jobs will appear here.");

    [HttpGet("/provider/wallet")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    public IActionResult ProviderWallet() => PortalSection("Provider", "Wallet", "Your earnings and payouts will appear here.");

    [HttpGet("/provider/profile")]
    [Authorize(Roles = UserTypeConstants.Provider)]
    public Task<IActionResult> ProviderProfile(CancellationToken cancellationToken) =>
        ProfileAsync("Provider", cancellationToken);

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

    private async Task<IActionResult> DashboardAsync(string role, CancellationToken cancellationToken)
    {
        var vm = await BuildVmAsync(cancellationToken);
        if (vm == null)
        {
            return Redirect("/login");
        }

        return View(role == "Provider" ? "ProviderDashboard" : "ClientDashboard", vm);
    }

    private async Task<IActionResult> ProfileAsync(string role, CancellationToken cancellationToken)
    {
        var vm = await BuildVmAsync(cancellationToken);
        if (vm == null)
        {
            return Redirect("/login");
        }

        ViewData["PortalRole"] = role;
        ViewData["SectionTitle"] = "Profile";
        ViewData["SectionBlurb"] = "Your account details.";
        return View("PortalSection", vm);
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

    private async Task<PublicDashboardVm?> BuildVmAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        var user = await _db.UsersLogins.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Uid == userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        return new PublicDashboardVm
        {
            FullName = User.Identity?.Name ?? "User",
            MobileNo = user.MobileNo,
            UserType = user.UserType,
            ProfileId = int.TryParse(User.FindFirstValue("ProfileId"), out var pid) ? pid : 0,
            LastLogin = user.LastLogin
        };
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HomeServicesPortal.Data;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;

namespace HomeServicesPortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IServicePillarService _pillars;

    private const string HomeStatsCacheKey = "home-public-stats";

    public HomeController(
        ILogger<HomeController> logger,
        IAuthService authService,
        AppDbContext db,
        IMemoryCache cache,
        IServicePillarService pillars)
    {
        _logger = logger;
        _authService = authService;
        _db = db;
        _cache = cache;
        _pillars = pillars;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Only staff belong in the admin portal; Client/Provider sessions go to their own dashboard.
        if (RedirectSignedInUser() is { } redirect)
        {
            return redirect;
        }

        return View(await GetHomeStatsAsync(cancellationToken));
    }

    /// <summary>
    /// Signed-in users have no use for the marketing pages: staff go to the admin portal, Clients and
    /// Providers to their dashboard. They log out to see the public site. Null when nobody is signed in.
    /// </summary>
    private IActionResult? RedirectSignedInUser()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userType = User.FindFirst("UserType")?.Value;
        if (string.Equals(userType, "Staff", StringComparison.OrdinalIgnoreCase)) return Redirect("/Admin");
        if (string.Equals(userType, "Client", StringComparison.OrdinalIgnoreCase)) return Redirect("/client/dashboard");
        if (string.Equals(userType, "Provider", StringComparison.OrdinalIgnoreCase)) return Redirect("/provider/dashboard");
        return null;
    }

    /// <summary>Cached for 10 minutes; null when the DB is unreachable so the page still renders without numbers.</summary>
    private async Task<HomeStatsViewModel?> GetHomeStatsAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(HomeStatsCacheKey, out HomeStatsViewModel? cached))
        {
            return cached;
        }

        try
        {
            var stats = new HomeStatsViewModel
            {
                ActiveCategories = await _db.ServiceCategories.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken)
            };
            _cache.Set(HomeStatsCacheKey, stats, TimeSpan.FromMinutes(10));
            return stats;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not load home page stats");
            return null;
        }
    }

    // Public landing pages for the three top-level services (catalogue read from the database).
    [HttpGet("/home-maintenance")]
    public Task<IActionResult> HomeMaintenance(CancellationToken cancellationToken)
        => ServicePillarPage("home-maintenance", cancellationToken);

    [HttpGet("/specialized-services")]
    public Task<IActionResult> SpecializedServices(CancellationToken cancellationToken)
        => ServicePillarPage("specialized-services", cancellationToken);

    [HttpGet("/property-and-legal-services")]
    public Task<IActionResult> PropertyAndLegalServices(CancellationToken cancellationToken)
        => ServicePillarPage("property-and-legal-services", cancellationToken);

    private async Task<IActionResult> ServicePillarPage(string slug, CancellationToken cancellationToken)
    {
        if (RedirectSignedInUser() is { } redirect)
        {
            return redirect;
        }

        var model = await _pillars.GetAsync(slug, cancellationToken);
        return model == null ? NotFound() : View("ServicePillar", model);
    }

    [Route("/about")]
    public IActionResult About()
    {
        if (RedirectSignedInUser() is { } redirect)
        {
            return redirect;
        }

        return View();
    }

    [Route("/privacy-policy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [Route("/terms-and-conditions")]
    public IActionResult Terms()
    {
        return View();
    }

    [HttpGet]
    [Route("/delete-account")]
    public IActionResult DeleteAccount()
    {
        return View(new DeleteAccountRequest());
    }

    [HttpPost]
    [Route("/delete-account")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("delete-account")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var (success, error, data, _) = await _authService.DeleteAccountAsync(request, cancellationToken);

        ViewBag.SubmittedMobileNo = request.MobileNo;

        if (!success || data == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Account deletion failed.");
            return View(new DeleteAccountRequest { MobileNo = request.MobileNo });
        }

        ViewBag.DeletionSucceeded = true;
        ViewBag.DeletedMobileNo = data.MobileNo;
        return View(new DeleteAccountRequest());
    }

    public IActionResult DownloadApp()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

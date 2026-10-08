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
    private readonly IProviderZoneService _zones;
    private readonly IMemoryCache _cache;

    private const string HomeStatsCacheKey = "home-public-stats";

    public HomeController(
        ILogger<HomeController> logger,
        IAuthService authService,
        AppDbContext db,
        IProviderZoneService zones,
        IMemoryCache cache)
    {
        _logger = logger;
        _authService = authService;
        _db = db;
        _zones = zones;
        _cache = cache;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return Redirect("/Admin");
        }

        return View(await GetHomeStatsAsync(cancellationToken));
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
                VerifiedProviders = await _db.Providers.AsNoTracking().CountAsync(p => p.IsVerified, cancellationToken),
                ActiveCategories = await _db.ServiceCategories.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken),
                ServiceZones = (await _zones.GetZoneOptionsAsync(cancellationToken)).Count
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

    [Route("/about")]
    public IActionResult About()
    {
        return View();
    }

    [Route("/privacy-policy")]
    public IActionResult Privacy()
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

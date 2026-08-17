using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models;

namespace HomeServicesPortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IApkManagementService _apkManagementService;
    private readonly IAuthService _authService;

    public HomeController(
        ILogger<HomeController> logger,
        IApkManagementService apkManagementService,
        IAuthService authService)
    {
        _logger = logger;
        _apkManagementService = apkManagementService;
        _authService = authService;
    }

    public IActionResult Index()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return Redirect("/Admin");
        }

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
        return View(_apkManagementService.GetCurrentApk());
    }

    public IActionResult DownloadApkFile()
    {
        var current = _apkManagementService.GetCurrentApk();
        if (current == null)
        {
            return NotFound();
        }

        var path = _apkManagementService.GetPhysicalPath(current.FileName);
        if (path == null)
        {
            return NotFound();
        }

        return PhysicalFile(path, "application/vnd.android.package-archive", current.FileName);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

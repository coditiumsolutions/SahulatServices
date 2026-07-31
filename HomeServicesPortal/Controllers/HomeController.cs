using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models;

namespace HomeServicesPortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IApkManagementService _apkManagementService;

    public HomeController(ILogger<HomeController> logger, IApkManagementService apkManagementService)
    {
        _logger = logger;
        _apkManagementService = apkManagementService;
    }

    public IActionResult Index()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return Redirect("/Admin");
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
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

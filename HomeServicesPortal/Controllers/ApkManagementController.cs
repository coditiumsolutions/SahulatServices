using HomeServicesPortal.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin")]
public class ApkManagementController : Controller
{
    private readonly IApkManagementService _service;

    public ApkManagementController(IApkManagementService service)
    {
        _service = service;
    }

    [HttpGet("/Admin/ApkManagement")]
    public IActionResult Index()
    {
        return View(_service.GetCurrentApk());
    }

    [HttpPost("/Admin/ApkManagement/Upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile apkFile, CancellationToken cancellationToken)
    {
        var (success, error) = await _service.UploadAsync(apkFile, cancellationToken);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to upload the APK.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "APK uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/ApkManagement/Download")]
    public IActionResult Download()
    {
        var current = _service.GetCurrentApk();
        if (current == null)
        {
            return NotFound();
        }

        var path = _service.GetPhysicalPath(current.FileName);
        if (path == null)
        {
            return NotFound();
        }

        return PhysicalFile(path, "application/vnd.android.package-archive", current.FileName);
    }

    [HttpPost("/Admin/ApkManagement/Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string fileName)
    {
        var (success, error) = _service.Delete(fileName);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete the APK.";
        }
        else
        {
            TempData["SuccessMessage"] = "APK deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}

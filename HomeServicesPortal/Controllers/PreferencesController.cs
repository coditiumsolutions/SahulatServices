using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class PreferencesController : Controller
{
    private readonly IPreferencesService _service;

    public PreferencesController(IPreferencesService service)
    {
        _service = service;
    }

    [HttpGet("/Admin/Preferences")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var use12Hour = await _service.GetUse12HourAsync(cancellationToken);
        return View(new PreferencesVm { Use12Hour = use12Hour });
    }

    [HttpPost("/Admin/Preferences")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PreferencesVm model, CancellationToken cancellationToken)
    {
        await _service.SetUse12HourAsync(model.Use12Hour, cancellationToken);
        TempData["SuccessMessage"] = "Preferences saved successfully.";
        return RedirectToAction(nameof(Index));
    }
}

using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ConfigurationsController : Controller
{
    private readonly IConfigurationEntryService _service;

    public ConfigurationsController(IConfigurationEntryService service)
    {
        _service = service;
    }

    [HttpGet("/Admin/Configurations")]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/Admin/Configurations/Create")]
    public IActionResult Create()
    {
        return View(new ConfigurationFormVm());
    }

    [HttpPost("/Admin/Configurations/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConfigurationFormVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create configuration.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Configuration '{model.ConfigKey}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Configurations/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Configurations/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Configurations/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ConfigurationFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update configuration.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Configuration '{model.ConfigKey}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Configurations/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Configurations/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete configuration.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = $"Configuration '{vm.ConfigKey}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

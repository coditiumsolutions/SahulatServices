using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ServiceProvidersController : Controller
{
    private readonly IServiceProviderService _service;

    public ServiceProvidersController(IServiceProviderService service)
    {
        _service = service;
    }

    [HttpGet("/ServiceProviders")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/ServiceProviders/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new ServiceProviderFormVm
        {
            Categories = await _service.GetCategoryOptionsAsync(cancellationToken)
        });
    }

    [HttpPost("/ServiceProviders/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceProviderFormVm model, CancellationToken cancellationToken)
    {
        model.Categories = await _service.GetCategoryOptionsAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create provider.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"S-Provider '{model.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ServiceProviders/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/ServiceProviders/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ServiceProviders/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceProviderFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        model.Categories = await _service.GetCategoryOptionsAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update provider.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"S-Provider '{model.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ServiceProviders/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ServiceProviders/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete provider.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = $"S-Provider '{vm.FullName}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

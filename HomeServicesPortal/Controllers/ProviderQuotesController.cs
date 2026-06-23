using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ProviderQuotesController : Controller
{
    private readonly IProviderQuoteService _service;

    public ProviderQuotesController(IProviderQuoteService service)
    {
        _service = service;
    }

    [HttpGet("/ProviderQuotes")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/ProviderQuotes/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _service.PopulateFormAsync(new ProviderQuoteFormVm(), cancellationToken));
    }

    [HttpPost("/ProviderQuotes/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderQuoteFormVm model, CancellationToken cancellationToken)
    {
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create provider quote.");
            return View(model);
        }

        TempData["SuccessMessage"] = "P-Quote created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderQuotes/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/ProviderQuotes/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderQuotes/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProviderQuoteFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update provider quote.");
            return View(model);
        }

        TempData["SuccessMessage"] = "P-Quote updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderQuotes/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderQuotes/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete provider quote.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = "P-Quote deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

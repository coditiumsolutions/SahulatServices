using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ProviderDocumentsController : Controller
{
    private readonly IProviderDocumentService _service;

    public ProviderDocumentsController(IProviderDocumentService service)
    {
        _service = service;
    }

    [HttpGet("/ProviderDocuments")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetListAsync(search, sort, sortDir, page, cancellationToken));
    }

    [HttpGet("/ProviderDocuments/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _service.PopulateFormAsync(new ProviderDocumentFormVm(), cancellationToken));
    }

    [HttpPost("/ProviderDocuments/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderDocumentFormVm model, CancellationToken cancellationToken)
    {
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create document.");
            return View(model);
        }

        TempData["SuccessMessage"] = "P-Document created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderDocuments/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/ProviderDocuments/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderDocuments/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProviderDocumentFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update document.");
            return View(model);
        }

        TempData["SuccessMessage"] = "P-Document updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderDocuments/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderDocuments/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete document.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = "P-Document deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

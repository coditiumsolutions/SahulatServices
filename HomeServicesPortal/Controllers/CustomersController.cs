using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class CustomersController : Controller
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service)
    {
        _service = service;
    }

    [HttpGet("/Admin/Clients")]
    [HttpGet("/Admin/Customers")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/Admin/Clients/Create")]
    [HttpGet("/Admin/Customers/Create")]
    public IActionResult Create()
    {
        return View(new CustomerFormVm());
    }

    [HttpPost("/Admin/Clients/Create")]
    [HttpPost("/Admin/Customers/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormVm model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create client.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Client '{model.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Clients/Details/{id:int}")]
    [HttpGet("/Admin/Customers/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Clients/Edit/{id:int}")]
    [HttpGet("/Admin/Customers/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Clients/Edit/{id:int}")]
    [HttpPost("/Admin/Customers/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update client.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Client '{model.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Clients/Delete/{id:int}")]
    [HttpGet("/Admin/Customers/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Clients/Delete/{id:int}")]
    [HttpPost("/Admin/Customers/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete client.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = $"Client '{vm.FullName}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

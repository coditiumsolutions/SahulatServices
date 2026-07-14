using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ServiceRequestsController : Controller
{
    private readonly IServiceRequestService _service;
    private readonly IBookingService _bookingService;

    public ServiceRequestsController(IServiceRequestService service, IBookingService bookingService)
    {
        _service = service;
        _bookingService = bookingService;
    }

    [HttpGet("/Admin/ServiceRequests")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/Admin/ServiceRequests/Addresses")]
    public async Task<IActionResult> Addresses(int clientUid, CancellationToken cancellationToken = default)
    {
        var options = await _service.GetAddressOptionsAsync(clientUid, cancellationToken);
        return Json(options.Select(o => new { value = o.Value, text = o.Text }));
    }

    [HttpGet("/Admin/ServiceRequests/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var vm = await _service.PopulateFormAsync(new ServiceRequestFormVm(), cancellationToken);
        return View(vm);
    }

    [HttpPost("/Admin/ServiceRequests/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRequestFormVm model, CancellationToken cancellationToken)
    {
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create service request.");
            return View(model);
        }

        TempData["SuccessMessage"] = "S-Request created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/ServiceRequests/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/ServiceRequests/Assign/{id:int}")]
    public async Task<IActionResult> Assign(int id, CancellationToken cancellationToken)
    {
        var vm = await _bookingService.GetAssignProviderFormAsync(id, cancellationToken);
        if (vm == null)
        {
            TempData["ErrorMessage"] = "Request not found, not pending, or already assigned.";
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    [HttpPost("/Admin/ServiceRequests/Assign/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, AssignProviderVm model, CancellationToken cancellationToken)
    {
        if (id != model.RequestUid) return BadRequest();

        var form = await _bookingService.GetAssignProviderFormAsync(id, cancellationToken);
        if (form == null)
        {
            return RedirectToAction(nameof(Index));
        }

        model.ClientName = form.ClientName;
        model.ServiceTitle = form.ServiceTitle;
        model.CategoryName = form.CategoryName;
        model.ServiceAddress = form.ServiceAddress;
        model.Status = form.Status;
        model.EstimatedBudget = form.EstimatedBudget;
        model.Providers = form.Providers;
        model.PaymentModeOptions = form.PaymentModeOptions;
        model.CommissionTypeOptions = form.CommissionTypeOptions;

        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _bookingService.AssignProviderAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to assign provider.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Provider assigned and booking created for request #{id}.";
        return RedirectToAction(nameof(Index));
    }
}

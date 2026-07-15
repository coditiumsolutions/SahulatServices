using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class PaymentsController : Controller
{
    private readonly IPaymentService _service;
    private readonly ICommissionRuleService _commissionRules;

    public PaymentsController(IPaymentService service, ICommissionRuleService commissionRules)
    {
        _service = service;
        _commissionRules = commissionRules;
    }

    [HttpGet("/Admin/Payments")]
    public IActionResult Index() => RedirectToAction(nameof(Ledger));

    [HttpGet("/Admin/Payments/Ledger")]
    public async Task<IActionResult> Ledger(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetLedgerListAsync(search, page, cancellationToken));
    }

    [HttpGet("/Admin/Payments/Ledger/Details/{id:int}")]
    public async Task<IActionResult> LedgerDetails(int id, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetLedgerDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Payments/Payouts")]
    public async Task<IActionResult> Payouts(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetPayoutListAsync(search, page, cancellationToken));
    }

    [HttpGet("/Admin/Payments/Payouts/Details/{id:int}")]
    public async Task<IActionResult> PayoutDetails(int id, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetPayoutDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Payments/CommissionRules")]
    public async Task<IActionResult> CommissionRules(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        return View(await _commissionRules.GetListAsync(search, page, cancellationToken));
    }

    [HttpGet("/Admin/Payments/CommissionRules/Create")]
    public async Task<IActionResult> CommissionRuleCreate(CancellationToken cancellationToken)
    {
        return View(await _commissionRules.PopulateFormAsync(new CommissionRuleFormVm(), cancellationToken));
    }

    [HttpPost("/Admin/Payments/CommissionRules/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CommissionRuleCreate(CommissionRuleFormVm model, CancellationToken cancellationToken)
    {
        await _commissionRules.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _commissionRules.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create commission rule.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Commission rule created successfully.";
        return RedirectToAction(nameof(CommissionRules));
    }

    [HttpGet("/Admin/Payments/CommissionRules/Details/{id:int}")]
    public async Task<IActionResult> CommissionRuleDetails(int id, CancellationToken cancellationToken)
    {
        var vm = await _commissionRules.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Payments/CommissionRules/Edit/{id:int}")]
    public async Task<IActionResult> CommissionRuleEdit(int id, CancellationToken cancellationToken)
    {
        var vm = await _commissionRules.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Payments/CommissionRules/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CommissionRuleEdit(int id, CommissionRuleFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        await _commissionRules.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _commissionRules.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update commission rule.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Commission rule updated successfully.";
        return RedirectToAction(nameof(CommissionRules));
    }

    [HttpGet("/Admin/Payments/CommissionRules/Delete/{id:int}")]
    public async Task<IActionResult> CommissionRuleDelete(int id, CancellationToken cancellationToken)
    {
        var vm = await _commissionRules.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Payments/CommissionRules/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CommissionRuleDeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _commissionRules.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _commissionRules.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete commission rule.");
            return View("CommissionRuleDelete", vm);
        }

        TempData["SuccessMessage"] = "Commission rule deleted successfully.";
        return RedirectToAction(nameof(CommissionRules));
    }

    [HttpGet("/Admin/Payments/PersonLedger")]
    public async Task<IActionResult> PersonLedger(string? search, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetPersonLedgerIndexAsync(search, cancellationToken));
    }

    [HttpGet("/Admin/Payments/PersonLedger/{providerUid:int}")]
    public async Task<IActionResult> PersonLedgerStatement(int providerUid, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetPersonLedgerAsync(providerUid, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Payments/PersonLedger/{providerUid:int}/Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonLedgerAdd(
        int providerUid,
        [Bind(Prefix = "AddEntry")] PersonLedgerAddEntryVm model,
        CancellationToken cancellationToken = default)
    {
        model.ProviderUid = providerUid;

        if (!ModelState.IsValid)
        {
            var invalidVm = await _service.GetPersonLedgerAsync(providerUid, cancellationToken);
            if (invalidVm == null) return NotFound();
            invalidVm.AddEntry = model;
            return View("PersonLedgerStatement", invalidVm);
        }

        var (success, error) = await _service.AddPersonLedgerEntryAsync(model, cancellationToken);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to add ledger entry.";
            return RedirectToAction(nameof(PersonLedgerStatement), new { providerUid });
        }

        TempData["SuccessMessage"] = $"Ledger entry added ({model.EntryType} {model.Amount:N2}).";
        return RedirectToAction(nameof(PersonLedgerStatement), new { providerUid });
    }
}
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;

    public AdminController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("/Admin/Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        DashboardVm vm = await _dashboardService.GetDashboardAsync(cancellationToken);
        return View(vm);
    }
}


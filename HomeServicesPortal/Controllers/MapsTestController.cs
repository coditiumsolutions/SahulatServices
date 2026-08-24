using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Internal-only test harness for the Maps/GPS feature (Phase 2) — lets staff validate reverse
/// geocoding, nearby-provider search, and live SignalR location tracking from the browser without
/// the Flutter app.
/// </summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
[Route("Admin/MapsTest")]
public class MapsTestController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}

using HomeServicesPortal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>The "24-hour time" switch on the client and provider profile pages (stored per browser).</summary>
[Authorize(Roles = UserTypeConstants.Client + "," + UserTypeConstants.Provider)]
public class TimePreferenceController : Controller
{
    [HttpPost("/preferences/time-format")]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string? format, string? returnTo)
    {
        if (format is TimeFormatPreference.TwelveHour or TimeFormatPreference.TwentyFourHour)
        {
            Response.Cookies.Append(TimeFormatPreference.UserCookieName, format, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });
            TempData["ProfileSuccess"] = format == TimeFormatPreference.TwentyFourHour
                ? "Times now show in 24-hour format."
                : "Times now show in 12-hour format.";
        }

        // Only ever return to a profile page, never to a caller-supplied address.
        return Redirect(string.Equals(returnTo, "provider", StringComparison.OrdinalIgnoreCase) ? "/provider/profile" : "/client/profile");
    }
}

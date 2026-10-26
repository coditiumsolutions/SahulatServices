using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Middleware;

/// <summary>
/// Lets a client or provider pick 12-hour or 24-hour times for themselves (the app keeps the same switch on the
/// device). The choice lives in a cookie, so it is per browser; the admin portal's setting stays the default and
/// is the only one the admin pages use.
/// </summary>
public class UserTimeFormatMiddleware
{
    private readonly RequestDelegate _next;

    public UserTimeFormatMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var isAdmin = path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/adminportal", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

        TimeFormatPreference.SetForRequest(
            !isAdmin && context.Request.Cookies.TryGetValue(TimeFormatPreference.UserCookieName, out var value) ? value : null);

        return _next(context);
    }
}

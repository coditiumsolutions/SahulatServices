using System.Security.Claims;
using HomeServicesPortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Helpers;

public static class ProviderSessionExtensions
{
    /// <summary>Re-issues the cookie with a new display name, keeping its other claims and lifetime.</summary>
    public static async Task RefreshNameClaimAsync(this HttpContext httpContext, string fullName)
    {
        var result = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result.Principal == null)
        {
            return;
        }

        var claims = result.Principal.Claims.Where(c => c.Type != ClaimTypes.Name).ToList();
        claims.Add(new Claim(ClaimTypes.Name, fullName));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, result.Properties);
    }

    /// <summary>The signed-in provider's Providers.UID: the ProfileId claim if it matches a provider, else looked up by user id.</summary>
    public static async Task<int?> ResolveProviderUidAsync(
        this ClaimsPrincipal user,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (int.TryParse(user.FindFirstValue("ProfileId"), out var profileId) && profileId > 0)
        {
            var exists = await db.Providers.AsNoTracking().AnyAsync(p => p.Uid == profileId, cancellationToken);
            if (exists)
            {
                return profileId;
            }
        }

        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        return await db.Providers.AsNoTracking()
            .Where(p => p.UserUid == userId)
            .Select(p => (int?)p.Uid)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Filters;

/// <summary>
/// The app's provider entry gate: only a verified provider (Providers.IsVerified) may open the provider
/// pages; everyone else is sent to /provider/pending. Checked on every request, so a provider whose
/// verification is reset (for example after replacing a CNIC image) is bounced out straight away.
/// </summary>
public class RequireVerifiedProviderFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;

    public RequireVerifiedProviderFilter(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ct = context.HttpContext.RequestAborted;
        var providerUid = await context.HttpContext.User.ResolveProviderUidAsync(_db, ct);
        if (providerUid == null)
        {
            context.Result = new RedirectResult("/login");
            return;
        }

        var verified = await _db.Providers.AsNoTracking()
            .Where(p => p.Uid == providerUid.Value)
            .Select(p => p.IsVerified)
            .FirstOrDefaultAsync(ct);
        if (!verified)
        {
            context.Result = new RedirectResult("/provider/pending");
            return;
        }

        await next();
    }
}

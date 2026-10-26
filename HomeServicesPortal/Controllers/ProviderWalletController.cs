using HomeServicesPortal.Data;
using HomeServicesPortal.Filters;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>Provider "Wallet" tab: ledger balance, pending payout and transaction history (read-only; payouts are made by staff).</summary>
[Authorize(Roles = UserTypeConstants.Provider)]
[TypeFilter(typeof(RequireVerifiedProviderFilter))]
public class ProviderWalletController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPaymentService _payments;
    private readonly ILogger<ProviderWalletController> _logger;

    public ProviderWalletController(AppDbContext db, IPaymentService payments, ILogger<ProviderWalletController> logger)
    {
        _db = db;
        _payments = payments;
        _logger = logger;
    }

    [HttpGet("/provider/wallet")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        ViewData["Title"] = "Wallet";
        try
        {
            var wallet = await _payments.GetProviderWalletAsync(providerUid.Value, cancellationToken);
            if (wallet != null)
            {
                // Most recent first, like the app.
                wallet.Transactions = wallet.Transactions.OrderByDescending(t => t.CreatedOn).ThenByDescending(t => t.Uid).ToList();
                return View("ProviderWallet", wallet);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load the wallet for provider {ProviderUid}.", providerUid);
        }

        return View("ProviderWallet", null);
    }
}

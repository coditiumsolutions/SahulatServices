using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/providers-wallet")]
[AllowAnonymous]
public class ProviderWalletApiController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public ProviderWalletApiController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Fetch a provider's wallet balance and transaction history.</summary>
    [HttpGet("{providerUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderWalletApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderWalletApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProviderWalletApiDto>>> GetWallet(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var wallet = await _paymentService.GetProviderWalletAsync(providerUid, cancellationToken);
        if (wallet == null)
        {
            return NotFound(ApiResponse<ProviderWalletApiDto>.Fail("Provider not found."));
        }

        var dto = new ProviderWalletApiDto
        {
            ProviderUid = wallet.ProviderUid,
            ProviderName = wallet.ProviderName,
            CategoryName = wallet.CategoryName,
            Balance = wallet.Balance,
            PendingPayoutTotal = wallet.PendingPayoutTotal,
            Transactions = wallet.Transactions.Select(t => new ProviderWalletTxnApiDto
            {
                Uid = t.Uid,
                CreatedOn = t.CreatedOn,
                BookingUid = t.BookingUid,
                Reason = t.Reason,
                SignedAmount = t.SignedAmount,
                RunningBalance = t.RunningBalance
            }).ToList()
        };

        return Ok(ApiResponse<ProviderWalletApiDto>.Ok(dto, "Provider wallet fetched successfully."));
    }
}

using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/clients-detail")]
[AllowAnonymous]
public class ClientsDetailApiController : ControllerBase
{
    private readonly IClientDetailService _service;

    public ClientsDetailApiController(IClientDetailService service)
    {
        _service = service;
    }

    /// <summary>Fetch client detail from Clients table.</summary>
    [HttpGet("{clientUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ClientDetailApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClientDetailApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientDetailApiDto>>> GetDetail(
        int clientUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data) = await _service.GetClientDetailAsync(clientUid, cancellationToken);

        if (!success || data == null)
        {
            return NotFound(ApiResponse<ClientDetailApiDto>.Fail(error ?? "Client not found."));
        }

        return Ok(ApiResponse<ClientDetailApiDto>.Ok(data, "Client detail fetched successfully."));
    }

    /// <summary>Edit client detail in Clients table (mobileNo is not editable here).</summary>
    [HttpPut("{clientUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ClientDetailApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClientDetailApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ClientDetailApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientDetailApiDto>>> EditDetail(
        int clientUid,
        [FromBody] UpdateClientDetailRequestDto request,
        CancellationToken cancellationToken)
    {
        if (clientUid != request.ClientUid)
        {
            return BadRequest(ApiResponse<ClientDetailApiDto>.Fail("ClientUid in URL and body must match."));
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ClientDetailApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.UpdateClientDetailAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<ClientDetailApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<ClientDetailApiDto>.Fail(error ?? "Failed to update client detail."));
        }

        return Ok(ApiResponse<ClientDetailApiDto>.Ok(data, "Client detail updated successfully."));
    }
}

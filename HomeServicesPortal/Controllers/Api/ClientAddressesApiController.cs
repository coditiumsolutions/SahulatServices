using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/client-addresses")]
[AllowAnonymous]
public class ClientAddressesApiController : ControllerBase
{
    private readonly IClientAddressService _service;

    public ClientAddressesApiController(IClientAddressService service)
    {
        _service = service;
    }

    /// <summary>Get client addresses, optionally filtered by client.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ClientAddressApiDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientAddressApiDto>>>> GetAll(
        [FromQuery] int? clientUid,
        CancellationToken cancellationToken)
    {
        var addresses = await _service.GetAddressesAsync(clientUid, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ClientAddressApiDto>>.Ok(
            addresses,
            addresses.Count == 0 ? "No client addresses found." : "Client addresses fetched successfully."));
    }

    /// <summary>Get a client address by id.</summary>
    [HttpGet("{addressUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientAddressApiDto>>> GetById(
        int addressUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data) = await _service.GetAddressByIdAsync(addressUid, cancellationToken);

        if (!success || data == null)
        {
            return NotFound(ApiResponse<ClientAddressApiDto>.Fail(error ?? "Client address not found."));
        }

        return Ok(ApiResponse<ClientAddressApiDto>.Ok(data, "Client address fetched successfully."));
    }

    /// <summary>Create a client address.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ClientAddressApiDto>>> Create(
        [FromBody] CreateClientAddressRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ClientAddressApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.CreateAddressAsync(request, cancellationToken);

        if (!success || data == null)
        {
            return BadRequest(ApiResponse<ClientAddressApiDto>.Fail(error ?? "Failed to create client address."));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { addressUid = data.Uid },
            ApiResponse<ClientAddressApiDto>.Ok(data, "Client address created successfully."));
    }

    /// <summary>Update a client address.</summary>
    [HttpPut("{addressUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ClientAddressApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClientAddressApiDto>>> Update(
        int addressUid,
        [FromBody] UpdateClientAddressRequestDto request,
        CancellationToken cancellationToken)
    {
        if (addressUid != request.AddressUid)
        {
            return BadRequest(ApiResponse<ClientAddressApiDto>.Fail("AddressUid in URL and body must match."));
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ClientAddressApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.UpdateAddressAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<ClientAddressApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<ClientAddressApiDto>.Fail(error ?? "Failed to update client address."));
        }

        return Ok(ApiResponse<ClientAddressApiDto>.Ok(data, "Client address updated successfully."));
    }

    /// <summary>Delete a client address.</summary>
    [HttpDelete("{addressUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int addressUid,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _service.DeleteAddressAsync(addressUid, cancellationToken);

        if (!success)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<object>.Fail(error));
            }

            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to delete client address."));
        }

        return Ok(ApiResponse<object>.Ok(new { addressUid }, "Client address deleted successfully."));
    }
}

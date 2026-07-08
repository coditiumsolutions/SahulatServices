using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/customer-service-requests")]
[AllowAnonymous]
public class CustomerServiceRequestsApiController : ControllerBase
{
    private readonly ICustomerServiceRequestService _service;

    public CustomerServiceRequestsApiController(ICustomerServiceRequestService service)
    {
        _service = service;
    }

    /// <summary>Get customer service requests, optionally filtered by client.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerServiceRequestApiDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerServiceRequestApiDto>>>> GetAll(
        [FromQuery] int? clientUid,
        CancellationToken cancellationToken)
    {
        var requests = await _service.GetRequestsAsync(clientUid, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerServiceRequestApiDto>>.Ok(
            requests,
            requests.Count == 0 ? "No service requests found." : "Service requests fetched successfully."));
    }

    /// <summary>Get a customer service request by id.</summary>
    [HttpGet("{requestUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerServiceRequestApiDto>>> GetById(
        int requestUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data) = await _service.GetRequestByIdAsync(requestUid, cancellationToken);

        if (!success || data == null)
        {
            return NotFound(ApiResponse<CustomerServiceRequestApiDto>.Fail(error ?? "Service request not found."));
        }

        return Ok(ApiResponse<CustomerServiceRequestApiDto>.Ok(data, "Service request fetched successfully."));
    }

    /// <summary>Create a customer service request.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CustomerServiceRequestApiDto>>> Create(
        [FromBody] CreateCustomerServiceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<CustomerServiceRequestApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.CreateRequestAsync(request, cancellationToken);

        if (!success || data == null)
        {
            return BadRequest(ApiResponse<CustomerServiceRequestApiDto>.Fail(error ?? "Failed to create service request."));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { requestUid = data.Uid },
            ApiResponse<CustomerServiceRequestApiDto>.Ok(data, "Service request created successfully."));
    }

    /// <summary>Update a customer service request.</summary>
    [HttpPut("{requestUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CustomerServiceRequestApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerServiceRequestApiDto>>> Update(
        int requestUid,
        [FromBody] UpdateCustomerServiceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (requestUid != request.RequestUid)
        {
            return BadRequest(ApiResponse<CustomerServiceRequestApiDto>.Fail("RequestUid in URL and body must match."));
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<CustomerServiceRequestApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.UpdateRequestAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<CustomerServiceRequestApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<CustomerServiceRequestApiDto>.Fail(error ?? "Failed to update service request."));
        }

        return Ok(ApiResponse<CustomerServiceRequestApiDto>.Ok(data, "Service request updated successfully."));
    }

    /// <summary>Delete a customer service request.</summary>
    [HttpDelete("{requestUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int requestUid,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _service.DeleteRequestAsync(requestUid, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse<object>.Fail(error ?? "Service request not found."));
        }

        return Ok(ApiResponse<object>.Ok(new { requestUid }, "Service request deleted successfully."));
    }
}

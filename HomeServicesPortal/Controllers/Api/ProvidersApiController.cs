using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/providers")]
[AllowAnonymous]
public class ProvidersApiController : ControllerBase
{
    private readonly IServiceProviderService _service;

    public ProvidersApiController(IServiceProviderService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get service requests matching the provider's service category.
    /// </summary>
    /// <param name="providerUid">ProviderProfiles.UID</param>
    [HttpGet("{providerUid:int}/service-requests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>>> GetServiceRequests(
        int providerUid,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetServiceRequestsForProviderAsync(providerUid, cancellationToken);

        return response.Result switch
        {
            ProviderServiceRequestResult.ProviderNotFound => NotFound(
                ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>.Fail("Service provider not found.")),

            ProviderServiceRequestResult.CategoryNotAssigned => BadRequest(
                ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>.Fail("Provider has no service category assigned.")),

            _ when response.Items.Count == 0 => Ok(
                ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>.Ok(
                    response.Items,
                    "No matching service requests found.")),

            _ => Ok(
                ApiResponse<IReadOnlyList<ProviderServiceRequestApiDto>>.Ok(
                    response.Items,
                    "Service requests fetched successfully."))
        };
    }
}

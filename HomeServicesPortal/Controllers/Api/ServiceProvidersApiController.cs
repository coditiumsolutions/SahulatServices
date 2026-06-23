using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/service-providers")]
[AllowAnonymous]
public class ServiceProvidersApiController : ControllerBase
{
    private readonly IServiceProviderService _service;

    public ServiceProvidersApiController(IServiceProviderService service)
    {
        _service = service;
    }

    /// <summary>Get active service providers, optionally filtered by service category id.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceProviderApiDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceProviderApiDto>>> GetAll(
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var providers = await _service.GetActiveProvidersForApiAsync(categoryId, cancellationToken);
        return Ok(providers);
    }

    /// <summary>Get a single active service provider by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceProviderApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceProviderApiDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var provider = await _service.GetActiveProviderForApiAsync(id, cancellationToken);
        if (provider == null)
        {
            return NotFound(new { message = "Service provider not found." });
        }

        return Ok(provider);
    }
}

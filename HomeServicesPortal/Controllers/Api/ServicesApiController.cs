using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/services")]
[AllowAnonymous]
public class ServicesApiController : ControllerBase
{
    private readonly IServiceCatalogService _service;

    public ServicesApiController(IServiceCatalogService service)
    {
        _service = service;
    }

    /// <summary>Get all active top-level services.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceApiDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceApiDto>>> GetAll(CancellationToken cancellationToken)
    {
        var services = await _service.GetActiveServicesForApiAsync(cancellationToken);
        return Ok(services);
    }

    /// <summary>Get a single active service by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceApiDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var service = await _service.GetActiveServiceForApiAsync(id, cancellationToken);
        if (service == null)
        {
            return NotFound(new { message = "Service not found." });
        }

        return Ok(service);
    }
}

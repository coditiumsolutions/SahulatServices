using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/service-titles")]
[AllowAnonymous]
public class ServiceTitlesApiController : ControllerBase
{
    private readonly IServiceTitleService _service;

    public ServiceTitlesApiController(IServiceTitleService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get active service titles under a category.
    /// Required query: categoryUid.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceTitleApiDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceTitleApiDto>>> GetAll(
        [FromQuery] int categoryUid,
        CancellationToken cancellationToken)
    {
        var titles = await _service.GetActiveTitlesForApiAsync(categoryUid, cancellationToken);
        return Ok(titles);
    }

    /// <summary>Get a single active service title by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceTitleApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceTitleApiDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var title = await _service.GetActiveTitleForApiAsync(id, cancellationToken);
        if (title == null)
        {
            return NotFound(new { message = "Service title not found." });
        }

        return Ok(title);
    }
}

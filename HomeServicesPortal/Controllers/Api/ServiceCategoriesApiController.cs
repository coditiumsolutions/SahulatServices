using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/service-categories")]
[AllowAnonymous]
public class ServiceCategoriesApiController : ControllerBase
{
    private readonly IServiceCategoryService _service;

    public ServiceCategoriesApiController(IServiceCategoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get active service categories.
    /// Optional query: serviceUid — filter categories under a parent service.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceCategoryApiDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryApiDto>>> GetAll(
        [FromQuery] int? serviceUid,
        CancellationToken cancellationToken)
    {
        var categories = await _service.GetActiveCategoriesForApiAsync(serviceUid, cancellationToken);
        return Ok(categories);
    }

    /// <summary>Get a single active service category by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServiceCategoryApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceCategoryApiDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _service.GetActiveCategoryForApiAsync(id, cancellationToken);
        if (category == null)
        {
            return NotFound(new { message = "Service category not found." });
        }

        return Ok(category);
    }
}

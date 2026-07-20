using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers.Api;

[ApiController]
[Route("api/service-bookings")]
[AllowAnonymous]
public class ServiceBookingsApiController : ControllerBase
{
    private readonly IServiceBookingApiService _service;

    public ServiceBookingsApiController(IServiceBookingApiService service)
    {
        _service = service;
    }

    /// <summary>Get service bookings, optionally filtered by provider.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceBookingApiDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceBookingApiDto>>>> GetAll(
        [FromQuery] int? providerUid,
        CancellationToken cancellationToken)
    {
        var bookings = await _service.GetBookingsAsync(providerUid, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceBookingApiDto>>.Ok(
            bookings,
            bookings.Count == 0 ? "No bookings found." : "Bookings fetched successfully."));
    }

    /// <summary>Get a service booking by id, optionally scoped to a provider.</summary>
    [HttpGet("{bookingUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ServiceBookingApiDto>>> GetById(
        int bookingUid,
        [FromQuery] int? providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error, data) = await _service.GetBookingByIdAsync(bookingUid, providerUid, cancellationToken);

        if (!success || data == null)
        {
            return NotFound(ApiResponse<ServiceBookingApiDto>.Fail(error ?? "Booking not found."));
        }

        return Ok(ApiResponse<ServiceBookingApiDto>.Ok(data, "Booking fetched successfully."));
    }

    /// <summary>Create a service booking.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ServiceBookingApiDto>>> Create(
        [FromBody] CreateServiceBookingDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ServiceBookingApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.CreateBookingAsync(request, cancellationToken);

        if (!success || data == null)
        {
            return BadRequest(ApiResponse<ServiceBookingApiDto>.Fail(error ?? "Failed to create booking."));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { bookingUid = data.Uid },
            ApiResponse<ServiceBookingApiDto>.Ok(data, "Booking created successfully."));
    }

    /// <summary>Update a service booking.</summary>
    [HttpPut("{bookingUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ServiceBookingApiDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ServiceBookingApiDto>>> Update(
        int bookingUid,
        [FromBody] UpdateServiceBookingDto request,
        CancellationToken cancellationToken)
    {
        if (bookingUid != request.BookingUid)
        {
            return BadRequest(ApiResponse<ServiceBookingApiDto>.Fail("BookingUid in URL and body must match."));
        }

        if (!ModelState.IsValid)
        {
            var message = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                          ?? "Invalid request.";
            return BadRequest(ApiResponse<ServiceBookingApiDto>.Fail(message));
        }

        var (success, error, data) = await _service.UpdateBookingAsync(request, cancellationToken);

        if (!success || data == null)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<ServiceBookingApiDto>.Fail(error));
            }

            return BadRequest(ApiResponse<ServiceBookingApiDto>.Fail(error ?? "Failed to update booking."));
        }

        return Ok(ApiResponse<ServiceBookingApiDto>.Ok(data, "Booking updated successfully."));
    }

    /// <summary>Delete a service booking.</summary>
    [HttpDelete("{bookingUid:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int bookingUid,
        [FromQuery] int? providerUid,
        CancellationToken cancellationToken)
    {
        var (success, error) = await _service.DeleteBookingAsync(bookingUid, providerUid, cancellationToken);

        if (!success)
        {
            if (error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(ApiResponse<object>.Fail(error));
            }

            return BadRequest(ApiResponse<object>.Fail(error ?? "Failed to delete booking."));
        }

        return Ok(ApiResponse<object>.Ok(new { bookingUid }, "Booking deleted successfully."));
    }
}

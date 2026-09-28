using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Interfaces;

public interface INominatimService
{
    Task<(bool Success, string? Error, ReverseGeocodeResultDto? Data)> ReverseGeocodeAsync(
        decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, List<GeocodeSearchResultDto>? Data)> SearchGeocodeAsync(
        string query, int limit, CancellationToken cancellationToken = default);
}

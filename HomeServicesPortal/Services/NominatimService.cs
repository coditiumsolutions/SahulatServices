using System.Globalization;
using System.Text.Json;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Options;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Services;

/// <summary>
/// Reverse geocoding via OpenStreetMap Nominatim (free, no API key). Enforces sequential
/// process-wide throttling to respect Nominatim's ~1 request/second usage policy. A multi-instance
/// deployment would need a shared (e.g. Redis-backed) rate limiter instead — not needed at current scale.
/// </summary>
public class NominatimService : INominatimService
{
    private static readonly SemaphoreSlim Throttle = new(1, 1);
    private static DateTime _lastRequestUtc = DateTime.MinValue;

    private readonly HttpClient _httpClient;
    private readonly NominatimOptions _options;
    private readonly ILogger<NominatimService> _logger;

    public NominatimService(HttpClient httpClient, IOptions<NominatimOptions> options, ILogger<NominatimService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, ReverseGeocodeResultDto? Data)> ReverseGeocodeAsync(
        decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        await Throttle.WaitAsync(cancellationToken);
        try
        {
            var elapsedMs = (DateTime.UtcNow - _lastRequestUtc).TotalMilliseconds;
            var waitMs = _options.MinRequestIntervalMs - elapsedMs;
            if (waitMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(waitMs), cancellationToken);
            }

            try
            {
                var lat = latitude.ToString(CultureInfo.InvariantCulture);
                var lng = longitude.ToString(CultureInfo.InvariantCulture);
                var url = $"reverse?format=jsonv2&lat={lat}&lon={lng}&addressdetails=1";

                using var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nominatim reverse geocode failed with status {Status}", response.StatusCode);
                    return (false, "Unable to resolve address for the given coordinates.", null);
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out _))
                {
                    return (false, "Unable to resolve address for the given coordinates.", null);
                }

                var address = root.TryGetProperty("address", out var addressElement) ? addressElement : default;

                var result = new ReverseGeocodeResultDto
                {
                    DisplayName = root.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? string.Empty : string.Empty,
                    Road = GetAddressField(address, "road"),
                    Area = GetAddressField(address, "suburb") ?? GetAddressField(address, "neighbourhood"),
                    City = GetAddressField(address, "city") ?? GetAddressField(address, "town") ?? GetAddressField(address, "village"),
                    State = GetAddressField(address, "state"),
                    PostalCode = GetAddressField(address, "postcode"),
                    Latitude = latitude,
                    Longitude = longitude
                };

                return (true, null, result);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Nominatim reverse geocode request failed");
                return (false, "Unable to resolve address for the given coordinates.", null);
            }
            finally
            {
                _lastRequestUtc = DateTime.UtcNow;
            }
        }
        finally
        {
            Throttle.Release();
        }
    }

    private static string? GetAddressField(JsonElement address, string fieldName)
    {
        if (address.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return address.TryGetProperty(fieldName, out var value) ? value.GetString() : null;
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;

namespace HomeServicesPortal.Services;

public record StoreVersionResult(bool Success, string? Version, string? RawVersion, string? Error);

public interface IStoreVersionChecker
{
    /// <summary>Reads the version currently published in the store listing at <paramref name="storeUrl"/>.</summary>
    Task<StoreVersionResult> CheckAsync(string platform, string storeUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// iOS: Apple's public iTunes lookup API (official, stable). Android: Google Play has no public version API, so the
/// listing page is read and its embedded version string extracted; that is best-effort and can break if Google
/// changes the page, in which case the check fails with a clear message and nothing is saved.
/// </summary>
public partial class StoreVersionChecker : IStoreVersionChecker
{
    private readonly HttpClient _http;
    private readonly ILogger<StoreVersionChecker> _logger;

    public StoreVersionChecker(HttpClient http, ILogger<StoreVersionChecker> logger)
    {
        _http = http;
        _logger = logger;
    }

    [GeneratedRegex(@"^\d+\.\d+(\.\d+)?")]
    private static partial Regex LeadingVersion();

    [GeneratedRegex(@"[?&]id=([A-Za-z0-9._]+)")]
    private static partial Regex PlayPackage();

    [GeneratedRegex(@"/id(\d+)")]
    private static partial Regex AppleId();

    [GeneratedRegex(@"apps\.apple\.com/([a-z]{2})/")]
    private static partial Regex AppleCountry();

    // The page embeds the current version as [[["1.0.4"]]] (inside the AF_initDataCallback blob).
    [GeneratedRegex("\\[\\[\\[\"(\\d+\\.\\d+(?:\\.\\d+)?[^\"]{0,20})\"\\]\\]")]
    private static partial Regex PlayVersion();

    public async Task<StoreVersionResult> CheckAsync(string platform, string storeUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            return platform == "ios"
                ? await CheckAppStoreAsync(storeUrl, cancellationToken)
                : await CheckPlayStoreAsync(storeUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogWarning(ex, "Store version check failed for {Platform} ({Url}).", platform, storeUrl);
            return Fail("Could not read the store listing. Try again in a moment, or enter the version by hand.");
        }
    }

    private async Task<StoreVersionResult> CheckAppStoreAsync(string storeUrl, CancellationToken ct)
    {
        var id = AppleId().Match(storeUrl);
        if (!id.Success)
            return Fail("The iOS store URL in AppConfig has no app id (expected .../id1234567890).");
        var country = AppleCountry().Match(storeUrl);

        var url = $"https://itunes.apple.com/lookup?id={id.Groups[1].Value}" +
                  (country.Success ? $"&country={country.Groups[1].Value}" : string.Empty);
        using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
        var results = doc.RootElement.GetProperty("results");
        if (results.GetArrayLength() == 0)
            return Fail("The App Store did not return this app. Check the iOS store URL.");

        return Normalize(results[0].GetProperty("version").GetString());
    }

    private async Task<StoreVersionResult> CheckPlayStoreAsync(string storeUrl, CancellationToken ct)
    {
        var package = PlayPackage().Match(storeUrl);
        if (!package.Success)
            return Fail("The Android store URL in AppConfig has no package id (expected ...details?id=com.example.app).");

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://play.google.com/store/apps/details?id={package.Groups[1].Value}&hl=en&gl=pk");
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return Fail($"Google Play answered {(int)response.StatusCode} for this app.");

        var match = PlayVersion().Match(await response.Content.ReadAsStringAsync(ct));
        return match.Success
            ? Normalize(match.Groups[1].Value)
            : Fail("Could not find the version on the Google Play page (Google may have changed it). Enter it by hand.");
    }

    /// <summary>"1.0.6 - GPS" -> 1.0.6; "1.2" -> 1.2.0. latest_version is always major.minor.patch.</summary>
    private static StoreVersionResult Normalize(string? raw)
    {
        var m = LeadingVersion().Match((raw ?? string.Empty).Trim());
        if (!m.Success)
            return Fail($"The store returned \"{raw}\", which is not a version number.");

        var version = m.Value.Count(c => c == '.') == 1 ? m.Value + ".0" : m.Value;
        return new StoreVersionResult(true, version, raw, null);
    }

    private static StoreVersionResult Fail(string error) => new(false, null, null, error);
}

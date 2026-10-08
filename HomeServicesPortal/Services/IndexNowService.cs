using System.Net.Http.Json;
using System.Xml.Linq;

namespace HomeServicesPortal.Services;

public interface IIndexNowService
{
    /// <summary>
    /// Tells IndexNow (Bing and partner engines, which also feed ChatGPT search and Copilot) that these URLs are new
    /// or changed. Never throws; returns false if nothing was sent or the endpoint rejected the request.
    /// </summary>
    Task<bool> SubmitAsync(IReadOnlyCollection<string> urls, CancellationToken cancellationToken = default);

    /// <summary>Public page URLs listed in wwwroot/sitemap.xml, the single source of truth for what is submitted.</summary>
    IReadOnlyList<string> GetSitemapUrls();
}

public class IndexNowService : IIndexNowService
{
    public const string Endpoint = "https://api.indexnow.org/indexnow";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<IndexNowService> _logger;

    public IndexNowService(HttpClient http, IConfiguration config, IWebHostEnvironment env, ILogger<IndexNowService> logger)
    {
        _http = http;
        _config = config;
        _env = env;
        _logger = logger;
    }

    /// <summary>The key is public by design: the same value is served as /{key}.txt so engines can verify ownership.</summary>
    private string Key => _config["IndexNow:Key"] ?? IndexNowDefaults.Key;

    private string Host => _config["IndexNow:Host"] ?? IndexNowDefaults.Host;

    public IReadOnlyList<string> GetSitemapUrls()
    {
        try
        {
            var path = Path.Combine(_env.WebRootPath ?? string.Empty, "sitemap.xml");
            if (!File.Exists(path))
            {
                return Array.Empty<string>();
            }

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            return XDocument.Load(path)
                .Descendants(ns + "loc")
                .Select(e => e.Value.Trim())
                .Where(u => u.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IndexNow: could not read sitemap.xml");
            return Array.Empty<string>();
        }
    }

    public async Task<bool> SubmitAsync(IReadOnlyCollection<string> urls, CancellationToken cancellationToken = default)
    {
        // Only URLs on our own host are allowed by the protocol.
        var list = urls
            .Where(u => Uri.TryCreate(u, UriKind.Absolute, out var uri)
                        && uri.Host.Equals(Host, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10000)
            .ToList();

        if (list.Count == 0)
        {
            return false;
        }

        var payload = new
        {
            host = Host,
            key = Key,
            keyLocation = $"https://{Host}/{Key}.txt",
            urlList = list
        };

        try
        {
            using var response = await _http.PostAsJsonAsync(Endpoint, payload, cancellationToken);
            // 200 = accepted, 202 = accepted and key validation pending. Anything else is logged, never thrown.
            if ((int)response.StatusCode is 200 or 202)
            {
                _logger.LogInformation("IndexNow: submitted {Count} URL(s), status {Status}", list.Count, (int)response.StatusCode);
                return true;
            }

            _logger.LogWarning("IndexNow: submission rejected with status {Status}", (int)response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "IndexNow: submission failed");
            return false;
        }
    }
}

public static class IndexNowDefaults
{
    // Public by design (served at /{Key}.txt). Override with IndexNow:Key / IndexNow:Host if ever rotated.
    public const string Key = "7b4f691ed30cc1b394265e78456b39f4";
    public const string Host = "sahulatghartak.com";
}

/// <summary>
/// Submits the public pages once shortly after the app starts in Production, i.e. after each deploy, because
/// a deploy is when page content changes. Runs in the background so it never delays startup.
/// </summary>
public class IndexNowStartupSubmitter : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public IndexNowStartupSubmitter(IServiceScopeFactory scopeFactory, IConfiguration config, IHostEnvironment env)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Production only by default; IndexNow:Enabled can force it on or off.
        var enabled = _config.GetValue<bool?>("IndexNow:Enabled") ?? _env.IsProduction();
        if (!enabled)
        {
            return;
        }

        try
        {
            // Let the site finish starting and be reachable before engines fetch the key file.
            await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var indexNow = scope.ServiceProvider.GetRequiredService<IIndexNowService>();
            await indexNow.SubmitAsync(indexNow.GetSitemapUrls(), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}

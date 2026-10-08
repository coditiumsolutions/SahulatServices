using System.Net;
using System.Text.Json;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

public class IndexNowTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public bool Throw { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Throw) throw new HttpRequestException("network down");
            return new HttpResponseMessage(Status);
        }
    }

    private sealed class TestEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Production;
    }

    private static (IndexNowService svc, StubHandler handler) Create(string webRoot = "")
    {
        var handler = new StubHandler();
        var svc = new IndexNowService(
            new HttpClient(handler),
            new ConfigurationBuilder().Build(),
            new TestEnv { WebRootPath = webRoot },
            NullLogger<IndexNowService>.Instance);
        return (svc, handler);
    }

    [Fact]
    public async Task Submit_posts_host_key_keyLocation_and_urls()
    {
        var (svc, handler) = Create();

        var ok = await svc.SubmitAsync(new[] { "https://sahulatghartak.com/", "https://sahulatghartak.com/about" });

        Assert.True(ok);
        Assert.Equal(IndexNowService.Endpoint, handler.Request!.RequestUri!.ToString());
        using var doc = JsonDocument.Parse(handler.Body!);
        var root = doc.RootElement;
        Assert.Equal("sahulatghartak.com", root.GetProperty("host").GetString());
        Assert.Equal(IndexNowDefaults.Key, root.GetProperty("key").GetString());
        Assert.Equal($"https://sahulatghartak.com/{IndexNowDefaults.Key}.txt", root.GetProperty("keyLocation").GetString());
        Assert.Equal(2, root.GetProperty("urlList").GetArrayLength());
    }

    [Fact]
    public async Task Submit_drops_other_hosts_and_duplicates_and_sends_nothing_when_empty()
    {
        var (svc, handler) = Create();

        var none = await svc.SubmitAsync(new[] { "https://example.com/x", "not a url" });
        Assert.False(none);
        Assert.Null(handler.Request);

        await svc.SubmitAsync(new[] { "https://sahulatghartak.com/a", "https://SAHULATGHARTAK.com/a", "https://example.com/b" });
        using var doc = JsonDocument.Parse(handler.Body!);
        Assert.Equal(1, doc.RootElement.GetProperty("urlList").GetArrayLength());
    }

    [Fact]
    public async Task Submit_accepts_202_and_returns_false_on_rejection_or_network_error_without_throwing()
    {
        var (svc, handler) = Create();
        var urls = new[] { "https://sahulatghartak.com/" };

        handler.Status = HttpStatusCode.Accepted;
        Assert.True(await svc.SubmitAsync(urls));

        handler.Status = HttpStatusCode.Forbidden;
        Assert.False(await svc.SubmitAsync(urls));

        handler.Throw = true;
        Assert.False(await svc.SubmitAsync(urls));
    }

    [Fact]
    public void GetSitemapUrls_reads_loc_entries_and_tolerates_a_missing_file()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var (missing, _) = Create(dir);
            Assert.Empty(missing.GetSitemapUrls());

            File.WriteAllText(Path.Combine(dir, "sitemap.xml"),
                "<?xml version=\"1.0\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" +
                "<url><loc>https://sahulatghartak.com/</loc></url><url><loc>https://sahulatghartak.com/about</loc></url></urlset>");
            var (svc, _) = Create(dir);
            Assert.Equal(new[] { "https://sahulatghartak.com/", "https://sahulatghartak.com/about" }, svc.GetSitemapUrls());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Key_file_is_published_with_the_same_key_the_service_sends()
    {
        var root = FindRepoRoot();
        var keyFile = Path.Combine(root, "HomeServicesPortal", "wwwroot", IndexNowDefaults.Key + ".txt");
        Assert.True(File.Exists(keyFile));
        Assert.Equal(IndexNowDefaults.Key, File.ReadAllText(keyFile).Trim());
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "HomeServicesPortal", "wwwroot")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("repo root not found");
    }
}

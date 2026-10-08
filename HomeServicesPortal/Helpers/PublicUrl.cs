namespace HomeServicesPortal.Helpers;

/// <summary>
/// Absolute URLs for public pages (canonical, Open Graph, schema). The site sits behind nginx, which does not
/// forward the original scheme, so Request.Scheme reads "http" in production; that put http:// canonicals in
/// the page. The production host is therefore always https and the www variant folds into the bare domain.
/// </summary>
public static class PublicUrl
{
    public const string ProductionHost = "sahulatghartak.com";

    public static string Base(HttpRequest request)
    {
        var host = request.Host.Host;
        if (host.Equals(ProductionHost, StringComparison.OrdinalIgnoreCase)
            || host.Equals("www." + ProductionHost, StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + ProductionHost;
        }

        return $"{request.Scheme}://{request.Host}";
    }
}

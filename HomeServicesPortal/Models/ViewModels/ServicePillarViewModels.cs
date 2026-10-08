namespace HomeServicesPortal.Models.ViewModels;

public class ServicePillarViewModel
{
    public string Slug { get; set; } = string.Empty;

    /// <summary>Page title (also used as the breadcrumb label).</summary>
    public string PageTitle { get; set; } = string.Empty;

    public string Heading { get; set; } = string.Empty;

    public string MetaDescription { get; set; } = string.Empty;

    public string Lead { get; set; } = string.Empty;

    /// <summary>The top-level Service name as stored in the database (e.g. "Home Maintenance").</summary>
    public string ServiceName { get; set; } = string.Empty;

    public List<string> Intro { get; set; } = new();

    public List<ServicePillarCategoryVm> Categories { get; set; } = new();

    public List<ServicePillarFaqVm> Faqs { get; set; } = new();

    public List<ServicePillarLinkVm> OtherPillars { get; set; } = new();
}

public class ServicePillarCategoryVm
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Verified providers in this category; null when below the display threshold so small numbers are never shown.</summary>
    public int? VerifiedProviders { get; set; }

    /// <summary>A few active service titles under this category (names only; prices stay in the app).</summary>
    public List<string> Titles { get; set; } = new();
}

public class ServicePillarFaqVm
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;
}

public class ServicePillarLinkVm
{
    public string Url { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

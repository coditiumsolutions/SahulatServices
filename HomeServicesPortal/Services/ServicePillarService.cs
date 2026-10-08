using System.Text.RegularExpressions;
using HomeServicesPortal.Data;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HomeServicesPortal.Services;

public interface IServicePillarService
{
    /// <summary>The public landing page for a top-level service, or null for an unknown slug.</summary>
    Task<ServicePillarViewModel?> GetAsync(string slug, CancellationToken cancellationToken = default);
}

/// <summary>Static, hand-written page copy for each public pillar page. The catalogue itself comes from the database.</summary>
public static class ServicePillarDefinitions
{
    public sealed record Definition(
        string Slug,
        string[] DbNames,
        string ServiceName,
        string PageTitle,
        string Heading,
        string MetaDescription,
        string Lead,
        string[] Intro,
        (string Q, string A)[] Faqs);

    private const string Areas = "Sahulat Ghar Tak currently serves Islamabad and Rawalpindi, with a focus on Bahria Town in both cities. We are expanding across Pakistan.";
    private const string Pricing = "Pricing is shown in the Sahulat Ghar Tak app before you confirm a booking. Prices depend on the job, so check the app for the current price of the service you need.";
    private const string Booking = "Download the Sahulat Ghar Tak app, choose the service you need, describe the job, and you are matched with an available, verified provider near your location. You then confirm your booking and follow its progress in the app.";

    public static readonly IReadOnlyList<Definition> All = new[]
    {
        new Definition(
            "home-maintenance",
            new[] { "Home Maintenance" },
            "Home Maintenance",
            "Home Maintenance Services in Islamabad & Rawalpindi",
            "Home Maintenance Services in Islamabad and Rawalpindi",
            "Book verified electricians, plumbers, AC technicians, cleaners, carpenters, painters and more in Bahria Town, Islamabad and Rawalpindi. See pricing in the app before you confirm.",
            "Everyday upkeep for your home, done by verified professionals you book from your phone.",
            new[]
            {
                "Home Maintenance covers the jobs that keep a home running: electrical work, plumbing, AC service and repair, cleaning, carpentry, painting and other repairs and renovation work.",
                "Each category below lists some of the services you can request. Open the app to see the full list, the price for your job, and the providers available near you."
            },
            new[]
            {
                ("What home maintenance services can I book?", "You can book electrical, plumbing, AC, cleaning, carpentry and painting work, along with the other repair and renovation categories listed on this page."),
                ("How do I book a home maintenance service?", Booking),
                ("How much do home maintenance services cost?", Pricing),
                ("Which areas do you serve?", Areas)
            }),
        new Definition(
            "specialized-services",
            new[] { "Specialized Services" },
            "Specialized Services",
            "Specialized Services in Islamabad & Rawalpindi",
            "Specialized Services in Islamabad and Rawalpindi",
            "Find specialist providers in Bahria Town, Islamabad and Rawalpindi for jobs beyond routine home upkeep. Book through the Sahulat Ghar Tak app and see pricing before you confirm.",
            "Expert help for jobs that need a specialist, from a single app.",
            new[]
            {
                "Specialized Services are for jobs beyond routine home upkeep, where you want someone who does that one thing well. The categories below show what is currently available.",
                "Open the app to see the services under each category, the price for your job, and the specialists available near you."
            },
            new[]
            {
                ("What are specialized services?", "They are services that go beyond routine home maintenance and need a specialist, for example the categories listed on this page."),
                ("How do I book a specialized service?", Booking),
                ("How much do specialized services cost?", Pricing),
                ("Which areas do you serve?", Areas)
            }),
        new Definition(
            "property-and-legal-services",
            new[] { "Property & Legal Services", "Property and Legal Services" },
            "Property and Legal Services",
            "Property & Legal Services in Islamabad & Rawalpindi",
            "Property and Legal Services in Islamabad and Rawalpindi",
            "Find professionals for property and legal needs in Bahria Town, Islamabad and Rawalpindi through the Sahulat Ghar Tak app. See pricing in the app before you confirm.",
            "Practical support when ownership and paperwork matter.",
            new[]
            {
                "Property and Legal Services connect you with independent professionals for property and paperwork needs. The categories below show what is currently available.",
                "Sahulat Ghar Tak is a booking platform. The professionals you book are independent, and we do not give legal advice ourselves."
            },
            new[]
            {
                ("What property and legal services can I book?", "The categories listed on this page, such as lawyer services and the property and tax-related services shown above."),
                ("Does Sahulat Ghar Tak give legal advice?", "No. Sahulat Ghar Tak connects you with independent professionals through the app. Any advice or representation comes from the professional you book."),
                ("How do I book a property or legal service?", Booking),
                ("How much do these services cost?", Pricing),
                ("Which areas do you serve?", Areas)
            })
    };

    public static Definition? Find(string? slug)
        => All.FirstOrDefault(d => d.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
}

public class ServicePillarService : IServicePillarService
{
    /// <summary>Provider counts under this number are never shown, so a page cannot advertise thin coverage.</summary>
    public const int MinProvidersToDisplay = 3;

    /// <summary>Sample service titles shown per category.</summary>
    public const int SampleTitlesPerCategory = 6;

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ServicePillarService> _logger;

    public ServicePillarService(AppDbContext db, IMemoryCache cache, ILogger<ServicePillarService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    private static readonly Regex PricingParenthetical = new(
        @"\s*\([^)]*\b(start\w*|advance|payment|from|price|rs|pkr)\b[^)]*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingPricingWords = new(
        @"\s+(started\s+from|starting\s+from|starting|from)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LeadingNumber = new(@"^\s*\d+\s*[.)]\s*", RegexOptions.Compiled);
    private static readonly Regex Symbols = new(@"[\p{So}\p{Cs}\uFE0F\u200D]+", RegexOptions.Compiled);

    /// <summary>
    /// Service titles are admin-typed and sometimes carry price hints ("(Starting)", "(advance Payment)"),
    /// list numbers or star icons. The public pages never show prices, so those are stripped.
    /// </summary>
    public static string CleanTitle(string? title)
    {
        var t = title ?? string.Empty;
        t = Symbols.Replace(t, " ");
        t = PricingParenthetical.Replace(t, string.Empty);
        t = LeadingNumber.Replace(t, string.Empty);
        t = TrailingPricingWords.Replace(t, string.Empty);
        return Regex.Replace(t, @"\s+", " ").Trim();
    }

    /// <summary>Provider count to show for a category, or null when it is below the threshold.</summary>
    public static int? DisplayableProviderCount(int verifiedProviders)
        => verifiedProviders >= MinProvidersToDisplay ? verifiedProviders : null;

    public async Task<ServicePillarViewModel?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        var def = ServicePillarDefinitions.Find(slug);
        if (def == null)
        {
            return null;
        }

        var cacheKey = "service-pillar:" + def.Slug;
        if (_cache.TryGetValue(cacheKey, out ServicePillarViewModel? cached) && cached != null)
        {
            return cached;
        }

        var vm = new ServicePillarViewModel
        {
            Slug = def.Slug,
            PageTitle = def.PageTitle,
            Heading = def.Heading,
            MetaDescription = def.MetaDescription,
            Lead = def.Lead,
            ServiceName = def.ServiceName,
            Intro = def.Intro.ToList(),
            Faqs = def.Faqs.Select(f => new ServicePillarFaqVm { Question = f.Q, Answer = f.A }).ToList(),
            OtherPillars = ServicePillarDefinitions.All
                .Where(o => o.Slug != def.Slug)
                .Select(o => new ServicePillarLinkVm { Url = "/" + o.Slug, Name = o.ServiceName })
                .ToList()
        };

        try
        {
            var service = await _db.Services.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IsActive && def.DbNames.Contains(s.ServiceName), cancellationToken);

            if (service != null)
            {
                var categories = await _db.ServiceCategories.AsNoTracking()
                    .Where(c => c.ServiceUid == service.Uid && c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new { c.Uid, c.CategoryName })
                    .ToListAsync(cancellationToken);
                var categoryUids = categories.Select(c => c.Uid).ToList();

                var titleRows = await _db.ServiceTitles.AsNoTracking()
                    .Where(t => t.IsActive && categoryUids.Contains(t.CategoryUid))
                    .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Title)
                    .Select(t => new { t.CategoryUid, t.Title })
                    .ToListAsync(cancellationToken);

                var providerCounts = await _db.ProviderCategories.AsNoTracking()
                    .Where(pc => categoryUids.Contains(pc.CategoryUid) && pc.Provider.IsVerified)
                    .GroupBy(pc => pc.CategoryUid)
                    .Select(g => new { CategoryUid = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CategoryUid, x => x.Count, cancellationToken);

                vm.Categories = categories.Select(c => new ServicePillarCategoryVm
                {
                    Name = c.CategoryName.Trim(),
                    VerifiedProviders = DisplayableProviderCount(providerCounts.GetValueOrDefault(c.Uid)),
                    Titles = titleRows
                        .Where(t => t.CategoryUid == c.Uid)
                        .Select(t => CleanTitle(t.Title))
                        .Where(t => t.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(SampleTitlesPerCategory)
                        .ToList()
                }).ToList();
            }

            _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Render the static copy without the catalogue rather than failing the page; not cached.
            _logger.LogWarning(ex, "Could not load catalogue for service page {Slug}", def.Slug);
        }

        return vm;
    }
}

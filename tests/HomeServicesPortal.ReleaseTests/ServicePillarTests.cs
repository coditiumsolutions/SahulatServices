using HomeServicesPortal.Services;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

public class ServicePillarTests
{
    [Theory]
    [InlineData("Full body waxing (Starting)", "Full body waxing")]
    [InlineData("Brochure/design/Printing (advance Payment)", "Brochure/design/Printing")]
    [InlineData("Letter Head (Advance adjusted in order)", "Letter Head")]
    [InlineData("Electric Geyser Installation Started From", "Electric Geyser Installation")]
    [InlineData("3. Umrah Plus Package \u2B50\u2B50\u2B50", "Umrah Plus Package")]
    [InlineData("  CCTV   Camera Installation ", "CCTV Camera Installation")]
    [InlineData("AC Installation", "AC Installation")]
    public void CleanTitle_strips_price_hints_numbering_and_symbols(string raw, string expected)
        => Assert.Equal(expected, ServicePillarService.CleanTitle(raw));

    [Theory]
    [InlineData(0, null)]
    [InlineData(2, null)]
    [InlineData(3, 3)]
    [InlineData(26, 26)]
    public void Provider_counts_below_three_are_never_displayed(int providers, int? expected)
        => Assert.Equal(expected, ServicePillarService.DisplayableProviderCount(providers));

    [Fact]
    public void Slugs_resolve_case_insensitively_and_unknown_slugs_do_not()
    {
        Assert.NotNull(ServicePillarDefinitions.Find("home-maintenance"));
        Assert.NotNull(ServicePillarDefinitions.Find("Specialized-Services"));
        Assert.NotNull(ServicePillarDefinitions.Find("property-and-legal-services"));
        Assert.Null(ServicePillarDefinitions.Find("services"));
        Assert.Null(ServicePillarDefinitions.Find(null));
    }

    [Fact]
    public void Page_copy_never_mentions_a_price()
    {
        foreach (var d in ServicePillarDefinitions.All)
        {
            var text = string.Join(" ", new[] { d.Heading, d.MetaDescription, d.Lead }.Concat(d.Intro).Concat(d.Faqs.Select(f => f.A)));
            Assert.DoesNotContain("PKR", text);
            Assert.DoesNotContain("Rs.", text);
        }
    }
}

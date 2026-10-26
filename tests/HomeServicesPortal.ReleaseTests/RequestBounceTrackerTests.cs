using HomeServicesPortal.Helpers;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

public class RequestBounceTrackerTests
{
    private static (HashSet<int> Bounced, string Value) Run(string? stored, params (int, string?)[] now)
        => RequestBounceTracker.Update(stored, now);

    [Fact]
    public void First_visit_never_flags_a_bounce()
    {
        var (bounced, value) = Run(null, (7, "Requested"));
        Assert.Empty(bounced);
        Assert.Equal("7:R", value);
    }

    [Theory]
    [InlineData("Assigned")]
    [InlineData("In Progress")]
    public void Assigned_or_started_then_requested_again_is_bounced(string before)
    {
        var (_, stored) = Run(null, (7, before));
        var (bounced, value) = Run(stored, (7, "Requested"));
        Assert.Contains(7, bounced);
        Assert.Equal("7:B", value);
    }

    [Fact]
    public void Notice_stays_while_requested_and_clears_once_assigned_again()
    {
        var (bounced, value) = Run("7:B", (7, "Requested"));
        Assert.Contains(7, bounced);

        (bounced, value) = Run(value, (7, "Assigned"));
        Assert.Empty(bounced);
        Assert.Equal("7:A", value);
    }

    [Fact]
    public void Finished_requests_are_forgotten_and_garbage_is_ignored()
    {
        var (bounced, value) = Run("7:A,x:y,9:Q,12", (7, "Completed"), (8, null));
        Assert.Empty(bounced);
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void Other_requests_in_the_cookie_are_kept_when_only_one_is_viewed()
    {
        var (_, value) = Run("5:A,6:R", (6, "Assigned"));
        Assert.Equal("6:A,5:A", value);
    }
}

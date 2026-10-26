namespace HomeServicesPortal.Helpers;

/// <summary>
/// "Your provider had to cancel" notice on the client's requests. The API has no flag for it: a request that was
/// Assigned or In Progress and is Requested again was bounced back. Like the app, the web remembers the last
/// progress stage it showed per request, here in a per-browser cookie (no database table).
/// Unlike the app, which shows the notice once, it stays until the request is assigned again.
/// </summary>
public static class RequestBounceTracker
{
    public const string CookiePrefix = "sgt_req_seen_";
    private const char Requested = 'R', Assigned = 'A', InProgress = 'P', Bounced = 'B';
    private const int MaxEntries = 60;

    public static string CookieName(int clientUid) => CookiePrefix + clientUid;

    /// <summary>Works out which requests are bounced back now and returns the updated cookie value.</summary>
    /// <param name="stored">The cookie value as read, or null.</param>
    /// <param name="current">(request uid, progress stage) for every request on the page; stage is null for cancelled.</param>
    public static (HashSet<int> Bounced, string NewValue) Update(string? stored, IEnumerable<(int Uid, string? Progress)> current)
    {
        var seen = Parse(stored);
        var bounced = new HashSet<int>();

        foreach (var (uid, progress) in current)
        {
            seen.TryGetValue(uid, out var last);
            var now = Code(progress);

            if (now == Requested && last is Assigned or InProgress or Bounced)
            {
                bounced.Add(uid);
                seen[uid] = Bounced;
            }
            else if (now is Requested or Assigned or InProgress)
            {
                seen[uid] = now;
            }
            else
            {
                // Completed, cancelled or unknown: nothing to remember.
                seen.Remove(uid);
            }
        }

        var kept = seen.OrderByDescending(kv => kv.Key).Take(MaxEntries)
            .Select(kv => $"{kv.Key}:{kv.Value}");
        return (bounced, string.Join(',', kept));
    }

    private static char Code(string? progress) => progress switch
    {
        "Requested" => Requested,
        "Assigned" => Assigned,
        "In Progress" => InProgress,
        _ => '-'
    };

    private static Dictionary<int, char> Parse(string? stored)
    {
        var map = new Dictionary<int, char>();
        if (string.IsNullOrWhiteSpace(stored)) return map;

        foreach (var part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = part.Split(':');
            if (bits.Length == 2 && int.TryParse(bits[0], out var uid) && bits[1].Length == 1
                && bits[1][0] is Requested or Assigned or InProgress or Bounced)
            {
                map[uid] = bits[1][0];
            }
        }
        return map;
    }
}

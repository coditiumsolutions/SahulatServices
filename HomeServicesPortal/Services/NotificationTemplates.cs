namespace HomeServicesPortal.Services;

/// <summary>
/// Every user-facing push/inbox string lives here so wording (or a future Urdu variant) changes in one place.
/// Each method returns (title, body). Titles are the banner headline: short, and a single leading emoji on the key
/// moments only (new job, accepted, cancelled, completed) so it adds colour without becoming noise. Bodies carry the
/// detail: what, and who.
/// </summary>
public static class NotificationTemplates
{
    public static (string Title, string Body) JobAssigned(string serviceTitle) =>
        ("🔔 New job request", $"{Clean(serviceTitle)}. Tap to view the details and respond.");

    public static (string Title, string Body) BookingAccepted(string? providerName, string serviceTitle) =>
        ("✅ Provider accepted",
         $"{Name(providerName, "A provider")} accepted your {Clean(serviceTitle)} request and will be in touch shortly.");

    public static (string Title, string Body) JobUnavailable(string serviceTitle) =>
        ("Job no longer available", $"{Clean(serviceTitle)} was accepted by another provider. We'll let you know about the next one.");

    public static (string Title, string Body) ProviderReassigning(string serviceTitle) =>
        ("Finding you another provider", $"We're arranging a new provider for your {Clean(serviceTitle)} request. We'll update you soon.");

    public static (string Title, string Body) BookingCancelledForClient(string serviceTitle, string? reason) =>
        ("❌ Booking cancelled", WithReason($"Your {Clean(serviceTitle)} booking was cancelled.", reason));

    public static (string Title, string Body) BookingCancelledForProvider(string serviceTitle, string? reason) =>
        ("❌ Booking cancelled", WithReason($"The {Clean(serviceTitle)} booking was cancelled.", reason));

    public static (string Title, string Body) JobStarted(string? providerName, string serviceTitle) =>
        ("Your job has started", $"{Name(providerName, "Your provider")} has started your {Clean(serviceTitle)} job.");

    public static (string Title, string Body) JobCompletedForClient(string serviceTitle) =>
        ("🎉 Job completed", $"Your {Clean(serviceTitle)} job is complete. Thank you for choosing Sahulat Ghar Tak!");

    public static (string Title, string Body) JobCompletedForProvider(string serviceTitle) =>
        ("🎉 Job completed", $"{Clean(serviceTitle)} is complete and your earnings have been recorded.");

    private static string Clean(string? text) => string.IsNullOrWhiteSpace(text) ? "service" : text.Trim();

    private static string Name(string? name, string fallback) => string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();

    private static string WithReason(string sentence, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? sentence : $"{sentence} Reason: {reason.Trim()}";
}

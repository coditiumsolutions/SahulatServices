namespace HomeServicesPortal.Services;

/// <summary>
/// Every user-facing push/inbox string lives here so wording (or a future Urdu variant) changes in one place.
/// Each method returns (title, body). Keep titles short — they are the banner headline.
/// </summary>
public static class NotificationTemplates
{
    public static (string Title, string Body) JobAssigned(string serviceTitle) =>
        ("New job request", $"{Clean(serviceTitle)} - tap to respond.");

    public static (string Title, string Body) BookingAccepted(string? providerName, string serviceTitle) =>
        ("Provider accepted your request",
         $"{Name(providerName, "A provider")} will handle your {Clean(serviceTitle)} request.");

    public static (string Title, string Body) JobUnavailable(string serviceTitle) =>
        ("Job no longer available", $"{Clean(serviceTitle)} was taken by another provider.");

    public static (string Title, string Body) ProviderReassigning(string serviceTitle) =>
        ("Finding you another provider", $"We're arranging another provider for your {Clean(serviceTitle)} request.");

    public static (string Title, string Body) BookingCancelledForClient(string serviceTitle, string? reason) =>
        ("Booking cancelled", WithReason($"Your {Clean(serviceTitle)} booking was cancelled.", reason));

    public static (string Title, string Body) BookingCancelledForProvider(string serviceTitle, string? reason) =>
        ("Booking cancelled", WithReason($"The {Clean(serviceTitle)} booking was cancelled.", reason));

    public static (string Title, string Body) JobStarted(string? providerName, string serviceTitle) =>
        ("Your job has started", $"{Name(providerName, "Your provider")} has started your {Clean(serviceTitle)} job.");

    public static (string Title, string Body) JobCompletedForClient(string serviceTitle) =>
        ("Job completed", $"Your {Clean(serviceTitle)} job is complete. Thank you for using Sahulat Ghar Tak!");

    public static (string Title, string Body) JobCompletedForProvider(string serviceTitle) =>
        ("Job completed", $"{Clean(serviceTitle)} is complete and your earnings have been recorded.");

    private static string Clean(string? text) => string.IsNullOrWhiteSpace(text) ? "service" : text.Trim();

    private static string Name(string? name, string fallback) => string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();

    private static string WithReason(string sentence, string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? sentence : $"{sentence} Reason: {reason.Trim()}";
}

namespace HomeServicesPortal.Options;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Path to the Firebase service-account JSON. Relative paths resolve against the content root.
    /// Falls back to the GOOGLE_APPLICATION_CREDENTIALS environment variable when empty.
    /// </summary>
    public string? ServiceAccountPath { get; set; }
}

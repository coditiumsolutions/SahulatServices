namespace HomeServicesPortal.Options;

/// <summary>
/// Temporary OTP delivery settings. Set IncludeInResponse=false once a real SMS gateway is live.
/// </summary>
public class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>
    /// When true, send-otp / resend-otp include the generated code in data.otp.
    /// When false, data.otp is null (SMS gateway is expected to deliver it).
    /// </summary>
    public bool IncludeInResponse { get; set; }
}

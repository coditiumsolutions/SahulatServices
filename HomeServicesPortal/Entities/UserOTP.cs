namespace HomeServicesPortal.Entities;

/// <summary>Maps to dbo.UserOTP — OTP rows keyed by MobileNo (no FK to UsersLogin).</summary>
public class UserOTP
{
    public int Uid { get; set; }

    public string MobileNo { get; set; } = string.Empty;

    public string OTPCode { get; set; } = string.Empty;

    public string OTPType { get; set; } = string.Empty;

    public DateTime ExpiryTime { get; set; }

    public bool IsVerified { get; set; }

    public int AttemptCount { get; set; }

    public int SentCount { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? VerifiedOn { get; set; }
}

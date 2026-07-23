namespace HomeServicesPortal.Interfaces;

public interface ISmsService
{
    Task<bool> SendOtpAsync(string mobileNo, string otp, CancellationToken cancellationToken = default);
}

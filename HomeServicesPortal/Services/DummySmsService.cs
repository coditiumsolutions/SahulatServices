using HomeServicesPortal.Interfaces;

namespace HomeServicesPortal.Services;

/// <summary>Development SMS stub — logs OTP to the console instead of sending SMS.</summary>
public class DummySmsService : ISmsService
{
    private readonly ILogger<DummySmsService> _logger;

    public DummySmsService(ILogger<DummySmsService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendOtpAsync(string mobileNo, string otp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DummySms] OTP for {MobileNo}: {Otp}", mobileNo, otp);
        Console.WriteLine($"[DummySms] OTP for {mobileNo}: {otp}");
        return Task.FromResult(true);
    }
}

namespace HomeServicesPortal.Services.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(int userId, string email, string role);
}

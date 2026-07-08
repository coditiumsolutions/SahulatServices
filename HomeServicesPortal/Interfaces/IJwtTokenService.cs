namespace HomeServicesPortal.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(int userId, string userType, string mobileNo);
}

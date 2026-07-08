namespace HomeServicesPortal.Helpers;

/// <summary>
/// Temporary plain-text passwords for development.
/// Still verifies legacy BCrypt hashes until JWT auth is re-enabled.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password) => password;

    public static bool Verify(string password, string passwordHash)
    {
        if (passwordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        return string.Equals(password, passwordHash, StringComparison.Ordinal);
    }
}

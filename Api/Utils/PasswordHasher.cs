using BCrypt.Net;

namespace Wacam.Api.Utils;

/// <summary>
/// BCrypt password hashing utility — never stores plain text passwords.
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hash a plain text password using BCrypt with default cost factor (12).
    /// </summary>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verify a plain text password against a stored BCrypt hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

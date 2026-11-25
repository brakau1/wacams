using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WACAMS.Api.Utils;

/// <summary>
/// JWT token creation with claims (employeeId, role, displayName).
/// </summary>
public static class TokenGenerator
{
    private const string AccessTokenSecretKey = "WACAMS-AccessToken-Secret-Key-MustBe-At-Least-32-Characters-Long!";
    private const int AccessTokenExpiryHours = 4;
    private const int RefreshTokenExpiryDays = 7;

    /// <summary>
    /// Generate an access token for the given employee.
    /// </summary>
    public static (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(
        string employeeId,
        string displayName,
        string email,
        string role,
        IEnumerable<string>? permissions = null)
    {
        var accessToken = CreateJwtToken(employeeId, displayName, email, role, permissions);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(AccessTokenExpiryHours);

        return (accessToken, refreshToken, expiresAt);
    }

    private static string CreateJwtToken(string employeeId, string displayName, string email, string role,
        IEnumerable<string>? permissions = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AccessTokenSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employeeId),
            new("employeeId", employeeId),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add permission claims
        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var token = new JwtSecurityToken(
            issuer: "WACAMS",
            audience: "WACAMS-API",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(AccessTokenExpiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a cryptographically secure random refresh token.
    /// </summary>
    public static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Extract claims from a JWT token string.
    /// </summary>
    public static ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AccessTokenSecretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "WACAMS",
                ValidAudience = "WACAMS-API",
                IssuerSigningKey = securityKey
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public static string GetEmployeeIdFromToken(ClaimsPrincipal principal)
    {
        return principal.FindFirst("employeeId")?.Value
               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? throw new InvalidOperationException("No employeeId claim found in token.");
    }

    public static string GetRoleFromToken(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value
               ?? throw new InvalidOperationException("No role claim found in token.");
    }

    public static IEnumerable<string> GetPermissionsFromToken(ClaimsPrincipal principal)
    {
        return principal.FindAll("permission").Select(c => c.Value);
    }
}

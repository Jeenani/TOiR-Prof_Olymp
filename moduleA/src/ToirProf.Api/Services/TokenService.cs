using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ToirProf.Domain;

namespace ToirProf.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string inputPassword, string storedHash);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var key = _config["Jwt:Key"] ?? "SuperSecretToirProfKey2027ForJwtValidationMustBe32CharsLong!";
        var issuer = _config["Jwt:Issuer"] ?? "ToirProfApi";
        var audience = _config["Jwt:Audience"] ?? "ToirProfClients";
        var durationMinutes = int.TryParse(_config["Jwt:DurationInMinutes"], out var m) ? m : 480;

        var expiresAt = DateTime.UtcNow.AddMinutes(durationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.LastName} {user.FirstName}".Trim()),
            new(ClaimTypes.Role, user.Role?.RoleCode ?? "operator"),
            new("user_id", user.UserId.ToString()),
            new("role_code", user.Role?.RoleCode ?? "operator"),
            new("role_id", user.RoleId.ToString())
        };

        if (user.WorkshopId.HasValue)
        {
            claims.Add(new Claim("workshop_id", user.WorkshopId.Value.ToString()));
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return (handler.WriteToken(token), expiresAt);
    }

    public string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public bool VerifyPassword(string inputPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;
        var computed = HashPassword(inputPassword);
        return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}

using System.Security.Claims;
using System.Text;
using eBook.Model.Auth;
using eBook.Model.Constants;
using eBook.Services.Database;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace eBook.Services.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            SecurityAlgorithms.HmacSha256);
    }

    public AuthResponse CreateAuthResponse(User user, IReadOnlyCollection<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtClaimNames.Sub, user.Id.ToString()),
            new(JwtClaimNames.Name, user.Username),
            new(JwtClaimNames.TokenVersion, user.TokenVersion.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(JwtClaimNames.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expiresAt,
            SigningCredentials = _credentials
        };

        return new AuthResponse
        {
            Token = new JsonWebTokenHandler().CreateToken(descriptor),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Username = user.Username,
            Roles = roles.ToList()
        };
    }
}

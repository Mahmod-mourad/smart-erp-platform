using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Constants;

namespace NexaFlow.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtSettings> options) : IJwtTokenService
{
    private readonly JwtSettings _settings = options.Value;

    public (string token, DateTime expiresAtUtc) GenerateAccessToken(
        Guid userId, string email, string fullName, Guid tenantId, IEnumerable<string> roles, IEnumerable<string>? permissions = null)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaims.TenantId, tenantId.ToString()),
            new(AppClaims.FullName, fullName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (permissions is not null)
        {
            claims.AddRange(permissions.Select(p => new Claim("Permission", p)));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}

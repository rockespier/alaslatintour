using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Auth.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlasApp.Api.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public LoginResultDto CreateAccessToken(AuthenticatedUserDto user, int tokenVersion, bool rememberMe)
    {
        var jwtOptions = options.Value;
        var expiresAtUtc = rememberMe
            ? DateTimeOffset.UtcNow.AddDays(jwtOptions.RememberMeExpirationDays)
            : DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenExpirationMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new("tipo", user.Tipo.ToString()),
            new("token_version", tokenVersion.ToString())
        };

        if (user.AdminRole.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.AdminRole.Value.ToString()));
            claims.Add(new Claim("admin_role", user.AdminRole.Value.ToString()));
        }

        if (user.CompetitorId.HasValue)
        {
            claims.Add(new Claim("competitor_id", user.CompetitorId.Value.ToString()));
        }

        var tokenDescriptor = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(tokenDescriptor);
        var expiresIn = Math.Max(1, (int)Math.Round((expiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds));

        return new LoginResultDto(accessToken, expiresIn, user);
    }
}

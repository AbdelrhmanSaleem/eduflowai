using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EduFlowAI.Identity.Application.Services;

internal sealed class JwtTokenService(
    UserManager<AppUser> userManager,
    IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    public async Task<AccessTokenResult> CreateAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = jwtOptions.Value;
        ValidateOptions(options);

        var roles = await userManager.GetRolesAsync(user);
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role =>
            new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAt,
            roles.ToArray());
    }

    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) ||
            string.IsNullOrWhiteSpace(options.Audience) ||
            string.IsNullOrWhiteSpace(options.SigningKey) ||
            Encoding.UTF8.GetByteCount(options.SigningKey) < 32 ||
            options.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT configuration is invalid. Configure Jwt:Issuer, " +
                "Jwt:Audience, a signing key of at least 32 bytes, and " +
                "a positive Jwt:AccessTokenMinutes value.");
        }
    }
}

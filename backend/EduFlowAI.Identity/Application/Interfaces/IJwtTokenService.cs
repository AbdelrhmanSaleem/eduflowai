using EduFlowAI.Identity.Domain.Entities;

namespace EduFlowAI.Identity.Application.Interfaces;

public interface IJwtTokenService
{
    Task<AccessTokenResult> CreateAsync( AppUser user, CancellationToken cancellationToken = default);
}

public sealed record AccessTokenResult( string AccessToken, string TokenType,DateTimeOffset ExpiresAtUtc,IReadOnlyCollection<string> Roles);
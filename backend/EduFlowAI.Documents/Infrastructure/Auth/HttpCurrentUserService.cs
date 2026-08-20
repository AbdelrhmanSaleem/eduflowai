using System.Security.Claims;
using EduFlowAI.Documents.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EduFlowAI.Documents.Infrastructure.Auth;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
}
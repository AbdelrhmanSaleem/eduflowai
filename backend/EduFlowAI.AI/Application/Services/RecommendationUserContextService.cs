using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.Identity.Application.DbContextAbstraction;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.AI.Application.Services;

public sealed class RecommendationUserContextService(
    IIdentityDbContext identityDbContext)
    : IRecommendationUserContextService
{
    public async Task<RecommendationUserContextDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return new RecommendationUserContextDto();
        }

        var userIdValue = userId.ToString();

        var profile = await identityDbContext.ApplicantProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userIdValue)
            .Select(profile => new RecommendationUserContextDto
            {
                Major = profile.Major,
                Faculty = profile.Faculty,
                University = profile.University,
                DegreeLevel = profile.DegreeLevel,
                GraduationYear = profile.GraduationYear,
                CumulativeGrade =
                    profile.CumulativeGrade.ToString(),
                HasProfileData = true
            })
            .FirstOrDefaultAsync(cancellationToken);

        return profile ?? new RecommendationUserContextDto();
    }
}
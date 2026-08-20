using EduFlowAI.Identity.Application.DbContextAbstraction;
using EduFlowAI.Identity.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Identity.Application.Services;

internal sealed class ApplicantProfileLocker(
    IIdentityDbContext dbContext) : IApplicantProfileLocker
{
    public async Task LockAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.ApplicantProfiles
            .SingleOrDefaultAsync(
                item => item.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException(
                "An applicant profile must exist before submission.");
        }

        if (profile.IsProfileLocked)
        {
            return;
        }

        profile.IsProfileLocked = true;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

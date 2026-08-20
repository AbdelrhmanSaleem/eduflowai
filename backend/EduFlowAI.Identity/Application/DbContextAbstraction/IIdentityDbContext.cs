using EduFlowAI.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Identity.Application.DbContextAbstraction;

public interface IIdentityDbContext
{
    DbSet<AppUser> AppUsers { get; }
    DbSet<ApplicantProfile> ApplicantProfiles { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
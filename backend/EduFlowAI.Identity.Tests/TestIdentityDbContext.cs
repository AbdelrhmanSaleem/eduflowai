using EduFlowAI.Identity.Application.DbContextAbstraction;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Identity.Tests;

internal sealed class TestIdentityDbContext(
    DbContextOptions<TestIdentityDbContext> options)
    : DbContext(options), IIdentityDbContext
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<ApplicantProfile> ApplicantProfiles =>
        Set<ApplicantProfile>();
}
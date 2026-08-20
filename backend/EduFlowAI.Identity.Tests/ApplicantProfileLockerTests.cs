using EduFlowAI.Identity.Application.Services;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Identity.Tests;

public sealed class ApplicantProfileLockerTests
{
    [Fact]
    public async Task LockAsync_PermanentlyMarksTheApplicantProfile()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid().ToString();
        dbContext.ApplicantProfiles.Add(CreateProfile(userId));
        await dbContext.SaveChangesAsync();
        var locker = new ApplicantProfileLocker(dbContext);

        await locker.LockAsync(userId);
        await locker.LockAsync(userId);

        var profile = await dbContext.ApplicantProfiles.SingleAsync();
        Assert.True(profile.IsProfileLocked);
    }

    [Fact]
    public async Task LockAsync_RejectsSubmissionWithoutAProfile()
    {
        await using var dbContext = CreateDbContext();
        var locker = new ApplicantProfileLocker(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => locker.LockAsync(Guid.NewGuid().ToString()));

        Assert.Contains("must exist", exception.Message);
    }

    private static TestIdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestIdentityDbContext(options);
    }

    internal static ApplicantProfile CreateProfile(string userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FullNameEn = "Karim Test",
        FullNameAr = "كريم تيست",
        NationalId = "30001010123456",
        Nationality = "EG",
        DateOfBirth = new DateOnly(2000, 1, 1),
        Gender = Gender.Male,
        University = "Test University",
        Faculty = "Engineering",
        DegreeLevel = "Bachelor",
        Major = "Computer Engineering",
        GraduationYear = 2024,
        CumulativeGrade = CumulativeGrade.VeryGood,
        MilitaryStatus = MilitaryStatus.Completed,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}

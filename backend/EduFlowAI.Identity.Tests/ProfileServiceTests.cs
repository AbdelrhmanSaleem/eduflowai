using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Services;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Identity.Tests;

public sealed class ProfileServiceTests
{
    [Fact]
    public async Task Put_RejectsArabicCharactersInEnglishName()
    {
        await using var dbContext = CreateDbContext();
        var user = await SeedLockedUserAsync(dbContext);
        var service = new ProfileService(dbContext);
        var request = CreateMatchingRequest() with
        {
            FullNameEn = "Karim \u0643\u0631\u064A\u0645"
        };

        var result = await service.UpdateAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityFailureKind.Validation, result.Failure!.Kind);
        Assert.Contains(
            nameof(UpdateProfileRequest.FullNameEn),
            result.Failure.Errors!.Keys);
    }

    [Fact]
    public async Task Put_RejectsEnglishCharactersInArabicName()
    {
        await using var dbContext = CreateDbContext();
        var user = await SeedLockedUserAsync(dbContext);
        var service = new ProfileService(dbContext);
        var request = CreateMatchingRequest() with
        {
            FullNameAr = "\u0643\u0631\u064A\u0645 Karim"
        };

        var result = await service.UpdateAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityFailureKind.Validation, result.Failure!.Kind);
        Assert.Contains(
            nameof(UpdateProfileRequest.FullNameAr),
            result.Failure.Errors!.Keys);
    }

    [Fact]
    public async Task Put_AcceptsUnicodeLatinAndArabicNames()
    {
        await using var dbContext = CreateDbContext();
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "unicode@example.com",
            Email = "unicode@example.com",
            IsActive = true,
            PreferredLanguage = "en",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync();
        var service = new ProfileService(dbContext);
        var request = CreateMatchingRequest() with
        {
            FullNameEn = "José A. Abdel‑Rahman OʼNeil",
            FullNameAr = "عَبْد الرحمن"
        };

        var result = await service.UpdateAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(request.FullNameEn, result.Value!.FullNameEn);
        Assert.Equal(request.FullNameAr, result.Value.FullNameAr);
    }

    [Fact]
    public async Task Put_AllowsMutableChangesWithUnchangedLockedLegacyName()
    {
        await using var dbContext = CreateDbContext();
        var user = await SeedLockedUserAsync(dbContext);
        user.ApplicantProfile!.FullNameEn = "Karim كريم";
        await dbContext.SaveChangesAsync();
        var service = new ProfileService(dbContext);
        var request = CreateMatchingRequest() with
        {
            FullNameEn = user.ApplicantProfile.FullNameEn,
            Address = "New address"
        };

        var result = await service.UpdateAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New address", result.Value!.Address);
        Assert.Equal("Karim كريم", result.Value.FullNameEn);
    }

    [Fact]
    public async Task Put_RejectsProtectedFieldChangesAfterLock()
    {
        await using var dbContext = CreateDbContext();
        var user = await SeedLockedUserAsync(dbContext);
        var service = new ProfileService(dbContext);
        var request = CreateMatchingRequest() with
        {
            FullNameEn = "Changed Name",
            Address = "New address"
        };

        var result = await service.UpdateAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            IdentityFailureKind.Conflict,
            result.Failure!.Kind);
        var profile = await dbContext.ApplicantProfiles.SingleAsync();
        Assert.Equal("Karim Test", profile.FullNameEn);
        Assert.Equal("Old address", profile.Address);
    }

    [Fact]
    public async Task Put_AllowsNonProtectedChangesAfterLock()
    {
        await using var dbContext = CreateDbContext();
        var user = await SeedLockedUserAsync(dbContext);
        var service = new ProfileService(dbContext);
        var request = CreateMatchingRequest() with
        {
            Address = "New address",
            Governorate = "Giza",
            PhoneNumber = "+201001234567",
            PreferredLanguage = "ar",
            GmailNotificationsEnabled = true
        };

        var result = await service.UpdateAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var savedUser = await dbContext.AppUsers
            .Include(item => item.ApplicantProfile)
            .SingleAsync();
        Assert.Equal("New address", savedUser.ApplicantProfile!.Address);
        Assert.Equal("Giza", savedUser.ApplicantProfile.Governorate);
        Assert.Equal("+201001234567", savedUser.PhoneNumber);
        Assert.Equal("ar", savedUser.PreferredLanguage);
        Assert.True(savedUser.GmailNotificationsEnabled);
        Assert.True(savedUser.ApplicantProfile.IsProfileLocked);
    }

    private static TestIdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestIdentityDbContext(options);
    }

    private static async Task<AppUser> SeedLockedUserAsync(
        TestIdentityDbContext dbContext)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "karim@example.com",
            Email = "karim@example.com",
            IsActive = true,
            PreferredLanguage = "en",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var profile = ApplicantProfileLockerTests.CreateProfile(user.Id);
        profile.Address = "Old address";
        profile.IsProfileLocked = true;
        profile.User = user;
        user.ApplicantProfile = profile;

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static UpdateProfileRequest CreateMatchingRequest() => new()
    {
        FullNameEn = "Karim Test",
        FullNameAr = "كريم تيست",
        NationalId = "30001010123456",
        Nationality = "EG",
        DateOfBirth = new DateOnly(2000, 1, 1),
        Gender = Gender.Male,
        Address = "Old address",
        University = "Test University",
        Faculty = "Engineering",
        DegreeLevel = "Bachelor",
        Major = "Computer Engineering",
        GraduationYear = 2024,
        CumulativeGrade = CumulativeGrade.VeryGood,
        MilitaryStatus = MilitaryStatus.Completed,
        PreferredLanguage = "en"
    };
}

//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using EduFlowAI.Admission.Application.Interfaces.Services;
//using EduFlowAI.AI.Application.DocumentVerification;
//using EduFlowAI.AI.Infrastructure.DocumentVerification;
//using EduFlowAI.Identity.Application.DTOs;
//using EduFlowAI.Identity.Application.Interfaces;

//namespace EduFlowAI.AI.Tests;

//public class DocumentVerificationContextReaderTests
//{
//    [Fact]
//    public async Task GetAsync_NationalId_MapsFullNameArNationalIdAndDateOfBirth()
//    {
//        var profile = BuildProfile(fullNameAr: "Ahmed Mohamed", nationalId: "29001011234567", dateOfBirth: new DateOnly(1990, 1, 1));
//        var reader = BuildReader(userId: "user-1", profile: profile);

//        var context = await reader.GetAsync(Guid.NewGuid(), "NationalId", CancellationToken.None);

//        Assert.Equal("Ahmed Mohamed", context.ExpectedFields["FullNameAr"]);
//        Assert.Equal("29001011234567", context.ExpectedFields["NationalId"]);
//        Assert.Equal("1990-01-01", context.ExpectedFields["DateOfBirth"]);
//        Assert.Equal(3, context.ExpectedFields.Count);
//    }

//    [Fact]
//    public async Task GetAsync_GraduationCertificate_MapsAcademicFields()
//    {
//        var profile = BuildProfile(
//            fullNameEn: "Ahmed Mohamed",
//            university: "Cairo University",
//            faculty: "Engineering",
//            major: "Computer Engineering",
//            graduationYear: 2023);
//        var reader = BuildReader(userId: "user-1", profile: profile);

//        var context = await reader.GetAsync(Guid.NewGuid(), "GraduationCertificate", CancellationToken.None);

//        Assert.Equal("Ahmed Mohamed", context.ExpectedFields["FullNameEn"]);
//        Assert.Equal("Cairo University", context.ExpectedFields["University"]);
//        Assert.Equal("Engineering", context.ExpectedFields["Faculty"]);
//        Assert.Equal("Computer Engineering", context.ExpectedFields["Major"]);
//        Assert.Equal("2023", context.ExpectedFields["GraduationYear"]);
//    }

//    [Fact]
//    public async Task GetAsync_UnsupportedDocumentType_ThrowsFinalException()
//    {
//        var reader = BuildReader(userId: "user-1", profile: BuildProfile());

//        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
//            reader.GetAsync(Guid.NewGuid(), "SomethingUnknown", CancellationToken.None));

//        Assert.Equal("DOCUMENT_CONTEXT_NOT_FOUND", ex.ErrorCode);
//    }

//    [Fact]
//    public async Task GetAsync_ProfileNotFound_ThrowsFinalException()
//    {
//        var reader = BuildReader(userId: "user-1", profile: null);

//        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
//            reader.GetAsync(Guid.NewGuid(), "NationalId", CancellationToken.None));

//        Assert.Equal("DOCUMENT_CONTEXT_NOT_FOUND", ex.ErrorCode);
//    }

//    [Fact]
//    public async Task GetAsync_ApplicationNotFound_ThrowsFinalException()
//    {
//        var reader = new DocumentVerificationContextReader(
//            new FakeApplicationAccessReader { ThrowOnGetUserId = true },
//            new FakeProfileService { ProfileToReturn = BuildProfile() });

//        var ex = await Assert.ThrowsAsync<DocumentVerificationFinalException>(() =>
//            reader.GetAsync(Guid.NewGuid(), "NationalId", CancellationToken.None));

//        Assert.Equal("DOCUMENT_CONTEXT_NOT_FOUND", ex.ErrorCode);
//    }

//    private static DocumentVerificationContextReader BuildReader(string userId, ProfileResponse? profile) =>
//        new(
//            new FakeApplicationAccessReader { UserIdToReturn = userId },
//            new FakeProfileService { ProfileToReturn = profile });

//    private static ProfileResponse BuildProfile(
//        string? fullNameEn = null,
//        string? fullNameAr = null,
//        string? nationalId = null,
//        DateOnly? dateOfBirth = null,
//        string? university = null,
//        string? faculty = null,
//        string? major = null,
//        int? graduationYear = null,
//        string? militaryStatus = null) =>
//        new(
//            UserId: "user-1",
//            Email: "user@example.com",
//            PhoneNumber: null,
//            PreferredLanguage: "en",
//            GmailNotificationsEnabled: false,
//            IsComplete: true,
//            IsProfileLocked: false,
//            FullNameEn: fullNameEn,
//            FullNameAr: fullNameAr,
//            NationalId: nationalId,
//            Nationality: "EG",
//            DateOfBirth: dateOfBirth,
//            Gender: "Male",
//            Address: null,
//            Governorate: null,
//            University: university,
//            Faculty: faculty,
//            DegreeLevel: "Bachelor",
//            Major: major,
//            GraduationYear: graduationYear,
//            CumulativeGrade: "Good",
//            MilitaryStatus: militaryStatus,
//            CreatedAt: null,
//            UpdatedAt: null);

//    private sealed class FakeApplicationAccessReader : IApplicationAcessReader
//    {
//        public string? UserIdToReturn { get; set; }
//        public bool ThrowOnGetUserId { get; set; }

//        public Task<bool> CanAccessDocumentsAsync(Guid applicationId, string userId, CancellationToken cancellationToken)
//            => Task.FromResult(true);

//        public Task<Guid> GetProgramIdAsync(Guid applicationId, CancellationToken cancellationToken)
//            => Task.FromResult(Guid.NewGuid());

//        public Task<string> GetApplicantUserIdAsync(Guid applicationId, CancellationToken cancellationToken)
//        {
//            if (ThrowOnGetUserId)
//            {
//                throw new InvalidOperationException("Application not found.");
//            }

//            return Task.FromResult(UserIdToReturn ?? "user-1");
//        }

//        public Task<Guid> GetApplicationIdForUserAsync(string applicantUserId, CancellationToken cancellationToken)
//            => Task.FromResult(Guid.NewGuid());
//    }

//    private sealed class FakeProfileService : IProfileService
//    {
//        public ProfileResponse? ProfileToReturn { get; set; }

//        public Task<IdentityOperationResult<ProfileResponse>> GetAsync(
//            string userId,
//            CancellationToken cancellationToken = default) =>
//            Task.FromResult(ProfileToReturn is null
//                ? IdentityOperationResult<ProfileResponse>.Fail(
//                    new IdentityFailure(IdentityFailureKind.NotFound, "Profile not found."))
//                : IdentityOperationResult<ProfileResponse>.Success(ProfileToReturn));

//        public Task<IdentityOperationResult<ProfileResponse>> UpdateAsync(
//            string userId,
//            UpdateProfileRequest request,
//            CancellationToken cancellationToken = default) =>
//            throw new NotImplementedException();
//    }
//}
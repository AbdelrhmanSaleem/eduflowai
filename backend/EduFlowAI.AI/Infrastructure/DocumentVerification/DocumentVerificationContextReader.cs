using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.AI.Application.DocumentVerification;
using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.AI.Infrastructure.DocumentVerification;

// Resolves the applicant fields Gemini should compare a document against.
// The field mapping per document type is a business decision confirmed with
// the Admission/Identity owners - see SEIF_IMPLEMENTATION_GUIDE.md section 1.
public sealed class DocumentVerificationContextReader : IDocumentVerificationContextReader
{
    private readonly IAdmissionDbContext _admissionDbContext;
    private readonly IProfileService _profileService;

    public DocumentVerificationContextReader(
        IProfileService profileService,
        IAdmissionDbContext admissionDbContext)
    {
        _profileService = profileService;
        _admissionDbContext = admissionDbContext;
    }

    public async Task<DocumentVerificationContext> GetAsync(
        Guid applicationId,
        string documentType,
        CancellationToken cancellationToken)
    {
        var userId = await _admissionDbContext.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId).Select(a => a.ApplicantUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DocumentVerificationFinalException(
                "DOCUMENT_CONTEXT_NOT_FOUND",
                "The application for this verification request could not be found.",
                attemptCount: 1);
        }

        var profileResult = await _profileService.GetAsync(userId, cancellationToken);
        if (!profileResult.IsSuccess || profileResult.Value is null)
        {
            throw new DocumentVerificationFinalException(
                "DOCUMENT_CONTEXT_NOT_FOUND",
                "The applicant profile for this verification request could not be found.",
                attemptCount: 1);
        }

        var expectedFields = BuildExpectedFields(documentType, profileResult.Value);

        return new DocumentVerificationContext(applicationId, documentType, expectedFields);
    }

    private static IReadOnlyDictionary<string, string> BuildExpectedFields(
        string documentType,
        ProfileResponse profile) => documentType switch
        {
            "NationalId" => RequiredMap(
                ("FullNameAr", profile.FullNameAr),
                ("NationalId", profile.NationalId),
                ("DateOfBirth", profile.DateOfBirth?.ToString("yyyy-MM-dd"))),

            "BirthCertificate" => RequiredMap(
                ("FullNameAr", profile.FullNameAr),
                ("DateOfBirth", profile.DateOfBirth?.ToString("yyyy-MM-dd"))),

            "GraduationCertificate" => RequiredMap(
                ("FullNameEn", profile.FullNameEn),
                ("University", profile.University),
                ("Faculty", profile.Faculty),
                ("Major", profile.Major),
                ("GraduationYear", profile.GraduationYear?.ToString())),

            "MilitaryCertificate" => RequiredMap(
                ("FullNameAr", profile.FullNameAr),
                ("MilitaryStatus", profile.MilitaryStatus)),

            _ => throw new DocumentVerificationFinalException(
                "DOCUMENT_TYPE_UNSUPPORTED",
                $"Document type '{documentType}' is not supported for verification.",
                attemptCount: 1)
        };

    private static IReadOnlyDictionary<string, string> RequiredMap(params(string name, string? value)[] fields)
    {
        var hasMissingValues = fields.Any(f => string.IsNullOrWhiteSpace(f.value));
        if (hasMissingValues)
        {
            throw new DocumentVerificationFinalException(
                "DOCUMENT_CONTEXT_INCOMPLETE",
                "The applicant profile is missing data required for document verification.",
                attemptCount: 1);
        }

        return fields.ToDictionary(
            field => field.name,
            field => field.value!,
            StringComparer.OrdinalIgnoreCase);
    }

}

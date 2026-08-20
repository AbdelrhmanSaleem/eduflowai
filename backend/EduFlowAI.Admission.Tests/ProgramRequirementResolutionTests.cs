using EduFlowAI.Admission.Application.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Tests;

public sealed class ProgramRequirementResolutionTests
{
    [Fact]
    public void Resolve_includes_general_and_matching_gender_requirements()
    {
        var programId = Guid.NewGuid();
        var requirements = new[]
        {
            Requirement(programId, DocumentType.NationalId, null),
            Requirement(programId, DocumentType.MilitaryCertificate, Gender.Male),
            Requirement(programId, DocumentType.BirthCertificate, Gender.Female)
        };

        var result = ProgramRequirementResolution.Resolve(
            requirements,
            programId,
            Gender.Male);

        Assert.Equal(
            new[] { DocumentType.NationalId, DocumentType.MilitaryCertificate },
            result);
    }

    [Fact]
    public void Resolve_excludes_requirements_from_another_program()
    {
        var programId = Guid.NewGuid();
        var requirements = new[]
        {
            Requirement(programId, DocumentType.NationalId, null),
            Requirement(Guid.NewGuid(), DocumentType.GraduationCertificate, null)
        };

        var result = ProgramRequirementResolution.Resolve(
            requirements,
            programId,
            Gender.Female);

        Assert.Equal(new[] { DocumentType.NationalId }, result);
    }

    [Fact]
    public void Resolve_collapses_general_and_gender_specific_duplicates()
    {
        var programId = Guid.NewGuid();
        var requirements = new[]
        {
            Requirement(programId, DocumentType.NationalId, null),
            Requirement(programId, DocumentType.NationalId, Gender.Female)
        };

        var result = ProgramRequirementResolution.Resolve(
            requirements,
            programId,
            Gender.Female);

        Assert.Equal(new[] { DocumentType.NationalId }, result);
    }

    private static ProgramDocumentRequirement Requirement(
        Guid programId,
        DocumentType documentType,
        Gender? gender)
    {
        return new ProgramDocumentRequirement
        {
            Id = Guid.NewGuid(),
            ProgramId = programId,
            DocumentType = documentType,
            RequiredForGender = gender
        };
    }
}

using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.Interfaces.Services;

public sealed record ProgramRequirementSet(
    Guid ProgramId,
    Gender ApplicantGender,
    IReadOnlyList<DocumentType> DocumentTypes);

/// <summary>
/// Resolves the effective document set for one program and applicant gender.
/// General and gender-specific rows for the same document type collapse to one item.
/// </summary>
public interface IProgramRequirementReader
{
    Task<ProgramRequirementSet?> ResolveAsync(
        Guid programId,
        Gender applicantGender,
        CancellationToken cancellationToken = default);
}

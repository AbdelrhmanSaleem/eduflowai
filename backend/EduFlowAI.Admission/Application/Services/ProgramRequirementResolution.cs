using System.Linq.Expressions;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.Services;

internal static class ProgramRequirementResolution
{
    internal static Expression<Func<ProgramDocumentRequirement, bool>> ForApplicant(
        Guid programId,
        Gender applicantGender)
    {
        return requirement =>
            requirement.ProgramId == programId &&
            (requirement.RequiredForGender == null ||
             requirement.RequiredForGender == applicantGender);
    }

    internal static IReadOnlyList<DocumentType> Resolve(
        IEnumerable<ProgramDocumentRequirement> requirements,
        Guid programId,
        Gender applicantGender)
    {
        return requirements
            .AsQueryable()
            .Where(ForApplicant(programId, applicantGender))
            .Select(requirement => requirement.DocumentType)
            .Distinct()
            .OrderBy(documentType => documentType)
            .ToList();
    }
}

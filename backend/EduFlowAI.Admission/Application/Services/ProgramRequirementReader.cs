using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services;

public sealed class ProgramRequirementReader : IProgramRequirementReader
{
    private readonly IAdmissionDbContext _dbContext;

    public ProgramRequirementReader(IAdmissionDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<ProgramRequirementSet?> ResolveAsync(
        Guid programId,
        Gender applicantGender,
        CancellationToken cancellationToken = default)
    {
        if (programId == Guid.Empty)
        {
            throw new ArgumentException("A program ID is required.", nameof(programId));
        }

        if (!Enum.IsDefined(applicantGender) || applicantGender == Gender.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(applicantGender),
                applicantGender,
                "Applicant gender must be Male or Female.");
        }

        bool programExists = await _dbContext.Programs
            .AsNoTracking()
            .AnyAsync(program => program.Id == programId, cancellationToken);

        if (!programExists)
        {
            return null;
        }

        var documentTypes = await _dbContext.ProgramDocumentRequirements
            .AsNoTracking()
            .Where(ProgramRequirementResolution.ForApplicant(
                programId,
                applicantGender))
            .Select(requirement => requirement.DocumentType)
            .Distinct()
            .OrderBy(documentType => documentType)
            .ToListAsync(cancellationToken);

        return new ProgramRequirementSet(programId, applicantGender, documentTypes);
    }
}

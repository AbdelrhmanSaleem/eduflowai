using EduFlowAI.Admission.Infrastructure.Seeding;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionSeedIdsTests
{
    [Fact]
    public void Stable_seed_ids_are_nonempty_and_unique()
    {
        var ids = new List<Guid>
        {
            AdmissionSeedIds.ItiInstitutionId,
            AdmissionSeedIds.NineMonthProgramId,
            AdmissionSeedIds.NationalIdRequirementId,
            AdmissionSeedIds.BirthCertificateRequirementId,
            AdmissionSeedIds.GraduationCertificateRequirementId,
            AdmissionSeedIds.MilitaryCertificateRequirementId,
            AdmissionSeedIds.ProfessionalDevelopmentCrmTrackId,
            AdmissionSeedIds.IntegratedSoftwareArchitectureTrackId
        };

        ids.AddRange(AdmissionTrackSeedCatalog.All.Select(track => track.Id));
        ids.AddRange(AdmissionBranchSeedCatalog.All.Select(branch => branch.Id));

        Assert.DoesNotContain(Guid.Empty, ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}

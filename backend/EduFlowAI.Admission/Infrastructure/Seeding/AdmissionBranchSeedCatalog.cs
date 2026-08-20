namespace EduFlowAI.Admission.Infrastructure.Seeding;

internal sealed record AdmissionBranchSeedDefinition(
    Guid Id,
    string Name,
    string? Governorate);

internal static class AdmissionBranchSeedCatalog
{
    internal static IReadOnlyList<AdmissionBranchSeedDefinition> All { get; } =
    [
        new(AdmissionSeedIds.SmartVillageBranchId, "Smart Village", "Giza"),
        new(AdmissionSeedIds.AlexandriaBranchId, "Alexandria", "Alexandria"),
        new(AdmissionSeedIds.IsmailiaBranchId, "Ismailia", null),
        new(AdmissionSeedIds.AssiutBranchId, "Assiut", "Assiut"),
        new(AdmissionSeedIds.MansouraBranchId, "Mansoura", "Dakahlia"),
        new(AdmissionSeedIds.MenofiaBranchId, "Menofia", null),
        new(AdmissionSeedIds.AswanBranchId, "Aswan", null),
        new(AdmissionSeedIds.MinyaBranchId, "Minya", null),
        new(AdmissionSeedIds.NewCapitalBranchId, "New Capital", null),
        new(AdmissionSeedIds.PortSaidBranchId, "Port Said", null),
        new(AdmissionSeedIds.TantaBranchId, "Tanta", null),
        new(AdmissionSeedIds.ZagazigBranchId, "Zagazig", "Sharqia")
    ];

    internal static AdmissionBranchSeedDefinition? Find(
        Guid internalId,
        string? name)
    {
        return All.FirstOrDefault(definition => definition.Id == internalId)
            ?? All.FirstOrDefault(definition =>
                StringComparer.OrdinalIgnoreCase.Equals(definition.Name, name));
    }

    internal static AdmissionBranchSeedDefinition? FindByName(string? name)
    {
        return All.FirstOrDefault(definition =>
            StringComparer.OrdinalIgnoreCase.Equals(definition.Name, name));
    }
}

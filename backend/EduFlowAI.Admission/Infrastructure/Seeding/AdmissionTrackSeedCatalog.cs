namespace EduFlowAI.Admission.Infrastructure.Seeding;

internal sealed record AdmissionTrackSeedDefinition(
    Guid Id,
    Guid OfficialTrackId,
    string Name,
    string Category,
    IReadOnlyList<string> Locations,
    int? TotalHours,
    string MinimumGrade,
    int SourceTrackNumber,
    IReadOnlyList<string>? LegacyNames = null,
    int? MaxYearsSinceGraduation = null)
{
    private const string IntakeUrl =
        "https://iti.gov.eg/intakes/de3fa682-88c3-45e1-aa0c-e42bf47d5071/tracks";

    internal string OfficialTrackUrl => $"{IntakeUrl}/{OfficialTrackId:D}";

    internal string? Description =>
        AdmissionTrackNarrativeCatalog.Get(SourceTrackNumber).Description;

    internal IReadOnlyList<string> PrerequisiteTopics =>
        AdmissionTrackNarrativeCatalog.Get(SourceTrackNumber)
            .PrerequisiteTopics;

    internal string EligibilitySummary =>
        AdmissionTrackNarrativeCatalog.Get(SourceTrackNumber)
            .EligibilitySummary;

    internal IReadOnlyList<string> SupportedLegacyNames => LegacyNames ?? [];
}

internal static class AdmissionTrackCategories
{
    internal const string IndustrialSystems = "Industrial Systems";
    internal const string ContentDevelopments = "Content Developments";
    internal const string InformationSystems = "Information Systems";
    internal const string Infrastructure =
        "Cyber Security, Cloud, and Infrastructure Services";
    internal const string SoftwareEngineering =
        "Software Engineering & Agentic AI Development";
    internal const string CognitiveAi =
        "Cognitive Computing and Artificial Intelligence";
    internal const string SoftwareTesting =
        "AI-Driven Software Testing & Validation";
}

internal static class AdmissionTrackMinimumGrades
{
    internal const string Pass = "Pass";
    internal const string Fair = "Fair";
    internal const string Good = "Good";
}

internal static class AdmissionTrackSeedCatalog
{
    internal const int Intake = 47;
    internal const int Year = 2026;

    internal static IReadOnlyList<AdmissionTrackSeedDefinition> All { get; } =
    [
        new(
            AdmissionSeedIds.DigitalIcDesignTrackId,
            Guid.Parse("bbb80029-50e7-45dd-fe28-08dbe75ac461"),
            "Digital IC Design",
            AdmissionTrackCategories.IndustrialSystems,
            ["Smart Village"],
            1398,
            AdmissionTrackMinimumGrades.Good,
            1),
        new(
            AdmissionSeedIds.IndustrialAutomationTrackId,
            Guid.Parse("59d2e6c7-7221-4024-fe29-08dbe75ac461"),
            "Industrial Automation",
            AdmissionTrackCategories.IndustrialSystems,
            ["Smart Village"],
            null,
            AdmissionTrackMinimumGrades.Good,
            2,
            MaxYearsSinceGraduation: 5),
        new(
            AdmissionSeedIds.TelcoCloudEngineeringTrackId,
            Guid.Parse("6fe70953-57eb-4108-e476-08ddbf895a42"),
            "Telco-Cloud Engineering",
            AdmissionTrackCategories.IndustrialSystems,
            ["Ismailia"],
            1333,
            AdmissionTrackMinimumGrades.Good,
            3),
        new(
            AdmissionSeedIds.EmbeddedEdgeArchitecturesTrackId,
            Guid.Parse("911e3ea8-e71d-42be-8915-08ddc45d0929"),
            "Embedded & Edge Architectures",
            AdmissionTrackCategories.IndustrialSystems,
            ["Smart Village"],
            1365,
            AdmissionTrackMinimumGrades.Good,
            4),
        new(
            AdmissionSeedIds.GameProgrammingTrackId,
            Guid.Parse("f84a5c67-29ae-4584-fe16-08dbe75ac461"),
            "Game Programming",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            1212,
            AdmissionTrackMinimumGrades.Pass,
            5),
        new(
            AdmissionSeedIds.GameArtTrackId,
            Guid.Parse("f8c443d7-7e5a-4f28-fe1d-08dbe75ac461"),
            "Game Art",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            2250,
            AdmissionTrackMinimumGrades.Pass,
            6),
        new(
            AdmissionSeedIds.VfxCompositingTrackId,
            Guid.Parse("081938a0-8844-4c43-fe20-08dbe75ac461"),
            "VFX Compositing",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            1308,
            AdmissionTrackMinimumGrades.Pass,
            7),
        new(
            AdmissionSeedIds.TwoDAnimationMotionGraphicsTrackId,
            Guid.Parse("694d8ce5-d1f5-4f18-fe2e-08dbe75ac461"),
            "2D Animation and Motion Graphics",
            AdmissionTrackCategories.ContentDevelopments,
            ["Alexandria"],
            1344,
            AdmissionTrackMinimumGrades.Pass,
            8),
        new(
            AdmissionSeedIds.ThreeDGeneralistTrackId,
            Guid.Parse("111116ff-5fb8-4bb2-569f-08dc1681e04a"),
            "3D Generalist",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            1392,
            AdmissionTrackMinimumGrades.Pass,
            9),
        new(
            AdmissionSeedIds.ThreeDAnimationTrackId,
            Guid.Parse("c395af87-08de-4ad0-56a0-08dc1681e04a"),
            "3D Animation",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            1353,
            AdmissionTrackMinimumGrades.Pass,
            10),
        new(
            AdmissionSeedIds.CgTechnicalDirectorTrackId,
            Guid.Parse("4d22d3a0-fa19-4018-a1fe-08dca49e88db"),
            "CG Technical Director",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            1404,
            AdmissionTrackMinimumGrades.Fair,
            11),
        new(
            AdmissionSeedIds.FurnitureDesignVisualizationTrackId,
            Guid.Parse("1e5bd9dc-0a7a-4272-a338-b363089cf1f3"),
            "Furniture Design & Visualization",
            AdmissionTrackCategories.ContentDevelopments,
            ["Smart Village"],
            1370,
            AdmissionTrackMinimumGrades.Good,
            12),
        new(
            AdmissionSeedIds.ArchitectureDesignVisualizationTrackId,
            Guid.Parse("9cb4c8a3-a0bf-4aac-b2f3-f130a7dd952f"),
            "Architecture Design & Visualization",
            AdmissionTrackCategories.InformationSystems,
            ["Smart Village"],
            1386,
            AdmissionTrackMinimumGrades.Good,
            13),
        new(
            AdmissionSeedIds.SystemsAdministrationTrackId,
            Guid.Parse("809b6241-1a01-4848-fde6-08dbe75ac461"),
            "Systems Administration",
            AdmissionTrackCategories.Infrastructure,
            ["Alexandria"],
            1236,
            AdmissionTrackMinimumGrades.Good,
            14),
        new(
            AdmissionSeedIds.CyberSecurityTrackId,
            Guid.Parse("def7af93-bc81-471a-fe03-08dbe75ac461"),
            "Cyber Security",
            AdmissionTrackCategories.Infrastructure,
            ["Smart Village"],
            1326,
            AdmissionTrackMinimumGrades.Good,
            15),
        new(
            AdmissionSeedIds.CloudArchitectureTrackId,
            Guid.Parse("540d81d0-5f53-4875-fe1c-08dbe75ac461"),
            "Cloud Architecture",
            AdmissionTrackCategories.Infrastructure,
            ["Smart Village", "Ismailia"],
            1155,
            AdmissionTrackMinimumGrades.Good,
            16),
        new(
            AdmissionSeedIds.GeospatialTechnologiesTrackId,
            Guid.Parse("2d24f998-e356-418b-fddf-08dbe75ac461"),
            "Geospatial Technologies",
            AdmissionTrackCategories.InformationSystems,
            ["Smart Village"],
            1227,
            AdmissionTrackMinimumGrades.Good,
            17),
        new(
            AdmissionSeedIds.ErpConsultingTrackId,
            Guid.Parse("f6e5a1b0-6659-4cb7-fde4-08dbe75ac461"),
            "ERP Consulting",
            AdmissionTrackCategories.InformationSystems,
            ["Smart Village"],
            1288,
            AdmissionTrackMinimumGrades.Good,
            18),
        new(
            AdmissionSeedIds.AecInformaticsTrackId,
            Guid.Parse("a414a379-621b-498e-fe25-08dbe75ac461"),
            "Architecture, Engineering and Construction Informatics",
            AdmissionTrackCategories.InformationSystems,
            ["Smart Village"],
            1308,
            AdmissionTrackMinimumGrades.Good,
            19),
        new(
            AdmissionSeedIds.DataManagementTrackId,
            Guid.Parse("ed19aabf-7ac0-43d5-fe2f-08dbe75ac461"),
            "Data Management",
            AdmissionTrackCategories.InformationSystems,
            ["Ismailia", "Smart Village"],
            1180,
            AdmissionTrackMinimumGrades.Good,
            20),
        new(
            AdmissionSeedIds.DataScienceTrackId,
            Guid.Parse("134636fe-7a41-49b0-fe32-08dbe75ac461"),
            "Data Science",
            AdmissionTrackCategories.InformationSystems,
            ["Smart Village"],
            1455,
            AdmissionTrackMinimumGrades.Good,
            21),
        new(
            AdmissionSeedIds.TelecomApplicationDevelopmentTrackId,
            Guid.Parse("03fd8892-7259-4340-fe1a-08dbe75ac461"),
            "Telecom Applications Development",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Smart Village"],
            1374,
            AdmissionTrackMinimumGrades.Good,
            22,
            ["Telecom Application Development"]),
        new(
            AdmissionSeedIds.OpenSourceApplicationsDevelopmentTrackId,
            Guid.Parse("c795d027-1aa2-4f8d-970f-2ecb2094353c"),
            "Open-Source Full-Stack Web Development with AI Integration",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Port Said", "Assiut", "Mansoura", "Minya", "Menofia", "Alexandria", "New Capital", "Zagazig"],
            1350,
            AdmissionTrackMinimumGrades.Good,
            23,
            ["Open Source Applications Development"]),
        new(
            AdmissionSeedIds.CrossPlatformMobileTrackId,
            Guid.Parse("eea9426e-ee08-40a3-bfbe-3e23de604656"),
            "Cross-Platform Mobile Applications Development with AI Integration",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Smart Village"],
            1437,
            AdmissionTrackMinimumGrades.Good,
            24),
        new(
            AdmissionSeedIds.NativeMobileTrackId,
            Guid.Parse("de0dcc16-ccf7-4ed1-8697-53cffe76301b"),
            "Native Mobile Applications Development with AI Integration",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Smart Village", "Alexandria", "Ismailia"],
            1287,
            AdmissionTrackMinimumGrades.Good,
            25),
        new(
            AdmissionSeedIds.DotNetEnterpriseTrackId,
            Guid.Parse("cf198247-911f-4df1-a66e-70beb76cfc09"),
            ".NET Enterprise Solutions Development & Architecture Foundations with AI Integration",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Assiut", "Aswan", "Smart Village", "Mansoura", "Menofia", "Alexandria", "Ismailia", "Tanta"],
            1313,
            AdmissionTrackMinimumGrades.Good,
            26),
        new(
            AdmissionSeedIds.FullStackWebUiTrackId,
            Guid.Parse("5f155664-4ecf-4a87-8546-855de2fa6f40"),
            "Full-Stack Web Development & UI Engineering with AI Integration",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Assiut", "Smart Village", "Ismailia"],
            1408,
            AdmissionTrackMinimumGrades.Good,
            27),
        new(
            AdmissionSeedIds.EnterpriseJavaTrackId,
            Guid.Parse("9feae0f6-aceb-403c-bfdb-94ac6180ab56"),
            "JAVA Enterprise & Cloud Native Development with AI Integration",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Smart Village"],
            1230,
            AdmissionTrackMinimumGrades.Good,
            28,
            ["Enterprise & Web Applications Development (Java)"]),
        new(
            AdmissionSeedIds.CloudPlatformDevelopmentTrackId,
            Guid.Parse("93be8556-2af9-4caa-ae1f-bb2af84bde56"),
            "Cloud Platform Development with AI-Assisted Operations",
            AdmissionTrackCategories.SoftwareEngineering,
            ["Smart Village"],
            1329,
            AdmissionTrackMinimumGrades.Good,
            29,
            ["Cloud Platform Development"]),
        new(
            AdmissionSeedIds.AiAndMachineLearningTrackId,
            Guid.Parse("a7f31797-bfe7-4ba5-fe30-08dbe75ac461"),
            "AI and Machine Learning",
            AdmissionTrackCategories.CognitiveAi,
            ["Smart Village", "Mansoura", "Alexandria", "Ismailia"],
            1329,
            AdmissionTrackMinimumGrades.Good,
            30),
        new(
            AdmissionSeedIds.AiDrivenSoftwareTestingTrackId,
            Guid.Parse("b72e71b3-baca-4dad-848a-3bc082934c31"),
            "AI-Driven Software Testing & Quality Assurance",
            AdmissionTrackCategories.SoftwareTesting,
            ["Smart Village"],
            1362,
            AdmissionTrackMinimumGrades.Good,
            31)
    ];

    internal static AdmissionTrackSeedDefinition? Find(
        Guid internalId,
        string? name)
    {
        if (AdmissionLegacyTrackCatalog.IsHistoricalId(internalId))
        {
            return null;
        }

        return All.FirstOrDefault(definition => definition.Id == internalId)
            ?? All.FirstOrDefault(definition =>
                StringComparer.OrdinalIgnoreCase.Equals(definition.Name, name));
    }

    internal static AdmissionTrackSeedDefinition? FindByName(string? name)
    {
        return All.FirstOrDefault(definition =>
            StringComparer.OrdinalIgnoreCase.Equals(definition.Name, name));
    }

    internal static bool IsCanonicalLocation(
        AdmissionTrackSeedDefinition definition,
        string branchName)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return definition.Locations.Contains(
            branchName,
            StringComparer.OrdinalIgnoreCase);
    }

    internal static bool IsCanonicalLocation(
        AdmissionTrackSeedDefinition definition,
        Guid branchId)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var branch = AdmissionBranchSeedCatalog.All
            .SingleOrDefault(candidate => candidate.Id == branchId);

        return branch is not null &&
            IsCanonicalLocation(definition, branch.Name);
    }
}

internal static class AdmissionLegacyTrackCatalog
{
    internal const string ProfessionalDevelopmentCrmName =
        "Professional Development & BI-infused CRM";
    internal const string IntegratedSoftwareArchitectureName =
        "Integrated Software Development & Architecture";

    internal static bool IsHistoricalId(Guid id) =>
        id == AdmissionSeedIds.ProfessionalDevelopmentCrmTrackId ||
        id == AdmissionSeedIds.IntegratedSoftwareArchitectureTrackId;

    internal static bool IsHistorical(Guid id, string? name) =>
        IsHistoricalId(id) ||
        StringComparer.OrdinalIgnoreCase.Equals(
            name,
            ProfessionalDevelopmentCrmName) ||
        StringComparer.OrdinalIgnoreCase.Equals(
            name,
            IntegratedSoftwareArchitectureName);
}

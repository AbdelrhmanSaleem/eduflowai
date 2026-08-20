using EduFlowAI.Admission.Infrastructure.Seeding;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionTrackSeedCatalogTests
{
    private static readonly string[] ExpectedNames =
    [
        "Digital IC Design",
        "Industrial Automation",
        "Telco-Cloud Engineering",
        "Embedded & Edge Architectures",
        "Game Programming",
        "Game Art",
        "VFX Compositing",
        "2D Animation and Motion Graphics",
        "3D Generalist",
        "3D Animation",
        "CG Technical Director",
        "Furniture Design & Visualization",
        "Architecture Design & Visualization",
        "Systems Administration",
        "Cyber Security",
        "Cloud Architecture",
        "Geospatial Technologies",
        "ERP Consulting",
        "Architecture, Engineering and Construction Informatics",
        "Data Management",
        "Data Science",
        "Telecom Applications Development",
        "Open-Source Full-Stack Web Development with AI Integration",
        "Cross-Platform Mobile Applications Development with AI Integration",
        "Native Mobile Applications Development with AI Integration",
        ".NET Enterprise Solutions Development & Architecture Foundations with AI Integration",
        "Full-Stack Web Development & UI Engineering with AI Integration",
        "JAVA Enterprise & Cloud Native Development with AI Integration",
        "Cloud Platform Development with AI-Assisted Operations",
        "AI and Machine Learning",
        "AI-Driven Software Testing & Quality Assurance"
    ];

    [Fact]
    public void Catalog_matches_the_official_intake_47_structure()
    {
        var tracks = AdmissionTrackSeedCatalog.All;

        Assert.Equal(ExpectedNames, tracks.Select(track => track.Name));
        Assert.Equal(31, tracks.Count);
        Assert.Equal(31, tracks.Select(track => track.Id).Distinct().Count());
        Assert.Equal(31, tracks.Select(track => track.OfficialTrackId).Distinct().Count());
        Assert.Equal(31, tracks.Select(track => track.OfficialTrackUrl).Distinct().Count());
        Assert.Equal(
            31,
            tracks.Select(track => track.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        Assert.Equal(7, tracks.Select(track => track.Category).Distinct().Count());
        Assert.Equal(54, tracks.Sum(track => track.Locations.Count));
        Assert.Equal(
            54,
            tracks.SelectMany(track => track.Locations.Select(location =>
                    $"{track.Name}\0{location}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        Assert.Equal(
            12,
            tracks.SelectMany(track => track.Locations)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [Fact]
    public void Catalog_has_the_canonical_grade_and_hours_edge_cases()
    {
        var tracks = AdmissionTrackSeedCatalog.All;

        Assert.Equal(
            24,
            tracks.Count(track =>
                track.MinimumGrade == AdmissionTrackMinimumGrades.Good));
        Assert.Equal(
            6,
            tracks.Count(track =>
                track.MinimumGrade == AdmissionTrackMinimumGrades.Pass));
        Assert.Equal(
            1,
            tracks.Count(track =>
                track.MinimumGrade == AdmissionTrackMinimumGrades.Fair));

        var industrialAutomation = Assert.Single(
            tracks,
            track => track.Name == "Industrial Automation");
        Assert.Null(industrialAutomation.TotalHours);
        Assert.Equal(5, industrialAutomation.MaxYearsSinceGraduation);

        Assert.All(
            tracks.Where(track => track.Name != "Industrial Automation"),
            track => Assert.True(track.TotalHours > 0));

        Assert.Equal(
            "CG Technical Director",
            Assert.Single(
                tracks,
                track => track.MinimumGrade == AdmissionTrackMinimumGrades.Fair)
                .Name);
    }

    [Fact]
    public void Catalog_has_complete_source_backed_recommendation_metadata()
    {
        foreach (var track in AdmissionTrackSeedCatalog.All)
        {
            Assert.NotEqual(Guid.Empty, track.Id);
            Assert.NotEqual(Guid.Empty, track.OfficialTrackId);
            Assert.StartsWith("https://iti.gov.eg/intakes/", track.OfficialTrackUrl);
            Assert.InRange(track.Name.Length, 1, 200);
            if (track.Name == "3D Generalist")
            {
                Assert.Null(track.Description);
            }
            else
            {
                Assert.InRange(track.Description!.Length, 1, 4000);
            }
            Assert.False(string.IsNullOrWhiteSpace(track.EligibilitySummary));
            Assert.All(
                track.PrerequisiteTopics,
                topic => Assert.InRange(topic.Length, 1, 100));
            Assert.Equal(
                track.PrerequisiteTopics.Count,
                track.PrerequisiteTopics
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
        }

        Assert.Empty(AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "Industrial Automation").PrerequisiteTopics);
        Assert.All(
            AdmissionTrackSeedCatalog.All.Where(track =>
                track.Name != "Industrial Automation"),
            track => Assert.NotEmpty(track.PrerequisiteTopics));
    }

    [Fact]
    public void Narrative_metadata_preserves_source_conflicts_and_utf8_text()
    {
        var gameProgramming = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "Game Programming");
        Assert.Equal(AdmissionTrackMinimumGrades.Pass, gameProgramming.MinimumGrade);
        Assert.Contains("minimum grade of Fair", gameProgramming.EligibilitySummary);

        var dataManagement = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "Data Management");
        Assert.Equal(
            "Not published by ITI for this track.",
            dataManagement.EligibilitySummary);

        Assert.Contains(
            "industry-grade specializations",
            AdmissionTrackSeedCatalog.All.Single(track =>
                track.Name == "Embedded & Edge Architectures").Description);
        Assert.Contains(
            "—",
            AdmissionTrackSeedCatalog.All.Single(track =>
                track.Name == "2D Animation and Motion Graphics").Description);

        string narrativeText = string.Join(
            ' ',
            AdmissionTrackSeedCatalog.All.SelectMany(track =>
                new[] { track.Description, track.EligibilitySummary }
                    .Where(value => value is not null)
                    .Concat(track.PrerequisiteTopics)));
        Assert.DoesNotContain("â", narrativeText);
        Assert.DoesNotContain("Ã", narrativeText);
        Assert.DoesNotContain("�", narrativeText);
    }

    [Fact]
    public void Branch_catalog_matches_the_twelve_official_locations()
    {
        string[] expected =
        [
            "Smart Village",
            "Alexandria",
            "Ismailia",
            "Assiut",
            "Mansoura",
            "Menofia",
            "Aswan",
            "Minya",
            "New Capital",
            "Port Said",
            "Tanta",
            "Zagazig"
        ];

        Assert.Equal(expected, AdmissionBranchSeedCatalog.All.Select(branch => branch.Name));
        Assert.Equal(
            12,
            AdmissionBranchSeedCatalog.All.Select(branch => branch.Id).Distinct().Count());
    }

    [Fact]
    public void Safe_legacy_renames_preserve_ids_but_ambiguous_tracks_do_not()
    {
        Assert.Equal(
            AdmissionSeedIds.OpenSourceApplicationsDevelopmentTrackId,
            AdmissionTrackSeedCatalog.All.Single(track =>
                track.Name.StartsWith("Open-Source", StringComparison.Ordinal)).Id);
        Assert.Equal(
            AdmissionSeedIds.CloudPlatformDevelopmentTrackId,
            AdmissionTrackSeedCatalog.All.Single(track =>
                track.Name.StartsWith("Cloud Platform", StringComparison.Ordinal)).Id);
        Assert.Equal(
            AdmissionSeedIds.EnterpriseJavaTrackId,
            AdmissionTrackSeedCatalog.All.Single(track =>
                track.Name.StartsWith("JAVA Enterprise", StringComparison.Ordinal)).Id);
        Assert.Equal(
            AdmissionSeedIds.TelecomApplicationDevelopmentTrackId,
            AdmissionTrackSeedCatalog.All.Single(track =>
                track.Name == "Telecom Applications Development").Id);

        Assert.DoesNotContain(
            AdmissionTrackSeedCatalog.All,
            track => AdmissionLegacyTrackCatalog.IsHistoricalId(track.Id));
        Assert.DoesNotContain(
            AdmissionTrackSeedCatalog.All,
            track => AdmissionLegacyTrackCatalog.IsHistorical(track.Id, track.Name));
    }
}

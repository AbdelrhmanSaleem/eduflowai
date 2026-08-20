using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class ProgramDocumentRequirementConfiguration : IEntityTypeConfiguration<ProgramDocumentRequirement>
    {
        public void Configure(EntityTypeBuilder<ProgramDocumentRequirement> builder)
        {
            builder.ToTable("ProgramDocumentRequirements", "admissions", table =>
            {
                table.HasCheckConstraint(
                    "CK_ProgramDocumentRequirement_DocumentType",
                    "\"DocumentType\" IN ('NationalId', 'BirthCertificate', 'GraduationCertificate', 'MilitaryCertificate')");

                table.HasCheckConstraint(
                    "CK_ProgramDocumentRequirement_RequiredForGender",
                    "\"RequiredForGender\" IS NULL OR \"RequiredForGender\" IN ('Male', 'Female')");
            });
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.RequiredForGender)
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.HasIndex(x => new { x.ProgramId, x.DocumentType })
                .HasDatabaseName("UX_ProgramDocumentRequirements_AllGenders")
                .HasFilter("\"RequiredForGender\" IS NULL")
                .IsUnique();

            builder.HasIndex(x => new { x.ProgramId, x.DocumentType, x.RequiredForGender })
                .HasDatabaseName("UX_ProgramDocumentRequirements_GenderScoped")
                .HasFilter("\"RequiredForGender\" IS NOT NULL")
                .IsUnique();

            builder.Property(x => x.CreatedAt)
                 .HasDefaultValueSql("now()")
                 .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasOne(x => x.Program)
                .WithMany(x => x.DocumentRequirements)
                .HasForeignKey(x => x.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

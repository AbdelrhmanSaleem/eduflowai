using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class ProgramConfiguration : IEntityTypeConfiguration<Program>
    {
        public void Configure(EntityTypeBuilder<Program> builder)
        {
            builder.ToTable("Programs", "admissions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200).IsRequired();

            builder.Property(x => x.Code)
                .HasMaxLength(30).IsRequired();

            builder.HasIndex(x => x.Code)
                .HasDatabaseName("UX_Programs_Code")
                .IsUnique();

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            // Prevent zero or negative duration
            builder.ToTable(t => t.HasCheckConstraint("CK_Program_Duration", "\"DurationMonths\" > 0"));

            // Relationship: Institution -> Programs (Restrict delete)
            builder.HasOne(x => x.Institution)
                .WithMany(x => x.Programs)
                .HasForeignKey(x => x.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

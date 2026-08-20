using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class CycleEligibilityRuleConfiguration : IEntityTypeConfiguration<CycleEligibilityRule>
    {
        public void Configure(EntityTypeBuilder<CycleEligibilityRule> builder)
        {
            builder.ToTable("CycleEligibilityRules", "admissions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RequiredNationality).HasMaxLength(5).IsRequired();
            builder.Property(x => x.RequiredDegreeLevel).HasMaxLength(30).IsRequired();
            builder.Property(x => x.MinGrade).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()").IsRequired();
            builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()").IsRequired();

            // Ensure valid years and grades
            builder.ToTable(t => t.HasCheckConstraint("CK_CycleEligibilityRule_MaxYears", "\"MaxYearsSinceGraduation\" >= 0"));
            builder.ToTable(t => t.HasCheckConstraint("CK_CycleEligibilityRule_MinGrade", "\"MinGrade\" IN ('Acceptable', 'Good', 'VeryGood', 'Excellent')"));

            // Relationship: Cycle -> EligibilityRule (1-to-1, Cascade delete)
            builder.HasOne(x => x.Cycle)
                .WithOne(x => x.EligibilityRule)
                .HasForeignKey<CycleEligibilityRule>(x => x.CycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.CycleId)
                .HasDatabaseName("UX_CycleEligibilityRules_Cycle")
                .IsUnique();
        }
    }
}

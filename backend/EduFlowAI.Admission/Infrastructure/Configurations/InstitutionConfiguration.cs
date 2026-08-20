using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
    {
        public void Configure(EntityTypeBuilder<Institution> builder)
        {
            builder.ToTable("Institutions", "admissions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200).IsRequired();

            builder.Property(x => x.Code)
                .HasMaxLength(20).IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasIndex(x => x.Code)
                .HasDatabaseName("UX_Institutions_Code")
                .IsUnique();
        }
    }
}

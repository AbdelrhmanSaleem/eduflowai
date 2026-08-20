using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class EnrollmentTaskLookupConfiguration : IEntityTypeConfiguration<EnrollmentTaskLookup>
    {
        public void Configure(EntityTypeBuilder<EnrollmentTaskLookup> builder)
        {
            // Set the table name explicitly
            builder.ToTable("EnrollmentTaskLookups");

            // Configure the primary key
            builder.HasKey(t => t.Id);

            // Configure properties
            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(255);

            // Store the TaskType enum as a string in the database
            builder.Property(t => t.TaskType)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(t => t.DisplayOrder)
                .IsRequired();

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}

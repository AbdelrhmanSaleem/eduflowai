using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class ApplicationEnrollmentTaskConfiguration : IEntityTypeConfiguration<ApplicationEnrollmentTask>
    {
        public void Configure(EntityTypeBuilder<ApplicationEnrollmentTask> builder)
        {
            // Set the table name explicitly
            builder.ToTable("ApplicationEnrollmentTasks");

            // Configure the primary key
            builder.HasKey(t => t.Id);

            // Store the Status enum as a string in the database
            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(t => t.Message)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(t => t.ActionUrl)
                .HasMaxLength(2048)
                .IsRequired(false);

            // Relationships

            // 1. One-to-Many with Application
            builder.HasOne(t => t.Application)
                .WithMany(a => a.EnrollmentTasks)
                .HasForeignKey(t => t.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting an application deletes its enrollment tasks

            // 2. Many-to-One with EnrollmentTaskLookup
            builder.HasOne(t => t.TaskLookup)
                .WithMany() // No navigation property required back to tasks
                .HasForeignKey(t => t.TaskId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a lookup task if applications are using it

            // Unique Constraint: An application cannot have the same task assigned twice
            builder.HasIndex(t => new { t.ApplicationId, t.TaskId })
                .IsUnique();
        }
    }
}

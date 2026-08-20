using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class ApplicationPreferenceConfiguration : IEntityTypeConfiguration<ApplicationPreference>
    {
        public void Configure(EntityTypeBuilder<ApplicationPreference> builder)
        {
            builder.ToTable("ApplicationPreferences", schema: "admissions", table =>
            {
                table.HasCheckConstraint(
                    "CK_ApplicationPreference_Rank",
                    "\"Rank\" IN (1, 2)");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

            builder.HasIndex(p => new { p.ApplicationId, p.Rank }).IsUnique();
            builder.HasIndex(p => new { p.ApplicationId, p.TrackBranchOfferingId }).IsUnique();

            builder.HasOne(p => p.Application)
                .WithMany(a => a.Preferences)
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.TrackBranchOffering)
                .WithMany(t => t.ApplicationPreferences)
                .HasForeignKey(p => p.TrackBranchOfferingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Infrastructure.Configurations
{
    public class ApplicantDocumentConfiguration : IEntityTypeConfiguration<ApplicantDocument>
    {
        public void Configure(EntityTypeBuilder<ApplicantDocument> builder)
        {
            builder.ToTable("ApplicantDocuments", schema: "documents", t =>
            {
                t.HasCheckConstraint("CK_ApplicantDocument_DocumentType",
                    "\"DocumentType\" IN ('NationalId','BirthCertificate','GraduationCertificate','MilitaryCertificate')");
                t.HasCheckConstraint("CK_ApplicantDocument_Status",
                    "\"Status\" IN ('Uploaded','Verifying','Approved','NeedsHumanReview','ReplacementRequested','Rejected')");
            });

            builder.HasKey(d => d.Id);

            builder.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(d => d.StorageKey).HasColumnType("text").IsRequired();
            builder.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
            builder.Property(d => d.VerificationDetailsJson).HasColumnType("jsonb");
            builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(d => d.UpdatedAt).HasDefaultValueSql("now()");

            builder.HasIndex(x => new
            {
                x.UpdatedAt,
                x.ApplicationId
            })
            .HasFilter("\"Status\" = 'NeedsHumanReview'")
            .HasDatabaseName("IX_ApplicantDocument_ReviewQueue");

            builder.HasIndex(d => new { d.ApplicationId, d.DocumentType }).IsUnique();
        }
    }
}

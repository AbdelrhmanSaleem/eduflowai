using EduFlowAI.Documents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Infrastructure.Configurations
{
    public class ApplicantDocumentVersionConfiguration:IEntityTypeConfiguration<ApplicantDocumentVersion>
    {
        public void Configure(
        EntityTypeBuilder<ApplicantDocumentVersion> builder)
        {
            builder.ToTable(
                "ApplicantDocumentVersions",
                "documents");

            builder.HasKey(version => version.Id);

            builder.Property(version => version.StorageKey)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(version => version.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(version => version.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(version => version.VerificationDetailsJson)
                .HasColumnType("jsonb");

            builder.HasIndex(version => new
            {
                version.DocumentId,
                version.VersionNumber
            }).IsUnique();

            builder.HasOne(version => version.Document)
                .WithMany(document => document.Versions)
                .HasForeignKey(version => version.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

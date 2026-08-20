using EduFlowAI.Documents.Domain.Entities;
using EduFlowAI.Documents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Infrastructure.Configurations
{
    public class DocumentReplacementRequestConfiguration : IEntityTypeConfiguration<DocumentReplacementRequest>
    {
        public void Configure(EntityTypeBuilder<DocumentReplacementRequest> builder)
        {
            builder.ToTable("DocumentReplacementRequests", schema: "documents", table =>
            {
                table.HasCheckConstraint(
                    "CK_DocumentReplacementRequest_Status",
                    "\"Status\" IN ('Open', 'Fulfilled')");
            });

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Reason)
                .HasColumnType("text").IsRequired();

            builder.Property(r => r.Status)
                .HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                //.HasDefaultValue(ReplacementRequestStatus.Open)
                .IsRequired();

            builder.Property(x => x.RequestedAt)
                .HasDefaultValueSql("now()");
            builder.HasIndex(r => r.DocumentId)
                .IsUnique()
                .HasFilter("\"Status\" = 'Open'")
                .HasDatabaseName("UX_DocumentReplacementRequest_OneOpen");

            builder.HasOne(r => r.Document)
                .WithMany(d => d.ReplacementRequests)
                .HasForeignKey(r => r.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}

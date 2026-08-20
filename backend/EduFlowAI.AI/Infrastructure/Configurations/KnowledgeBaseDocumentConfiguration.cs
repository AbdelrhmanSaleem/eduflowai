using EduFlowAI.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.AI.Infrastructure.Configurations
{
    public class KnowledgeBaseDocumentConfiguration : IEntityTypeConfiguration<KnowledgeBaseDocument>
    {
        public void Configure(EntityTypeBuilder<KnowledgeBaseDocument> builder)
        {
            builder.ToTable("KnowledgeBaseDocument", schema: "ai", table =>
            {
                table.HasCheckConstraint(
                    "CK_KnowledgeBaseDocument_Status",
                    "\"Status\" IN ('Pending', 'Indexing', 'Indexed', 'Failed')");
            });

            builder.HasKey(k => k.Id);

            builder.Property(k => k.FileName).HasMaxLength(500).IsRequired();
            builder.Property(k => k.StorageKey).HasColumnType("text").IsRequired();
            builder.Property(k => k.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(k => k.ErrorMessage).HasColumnType("text");
            builder.Property(k => k.CreatedAt).HasDefaultValueSql("now()");

            builder.HasIndex(k => k.UploadedByUserId);
        }
    }
}

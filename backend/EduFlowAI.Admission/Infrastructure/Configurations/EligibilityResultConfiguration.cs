using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class EligibilityResultConfiguration : IEntityTypeConfiguration<EligibilityResult>
    {
        public void Configure(EntityTypeBuilder<EligibilityResult> builder)
        {
            builder.ToTable("EligibilityResults", schema: "admissions", t =>
                t.HasCheckConstraint("CK_EligibilityResult_FailureReasonsJson_IsArray",
                    "jsonb_typeof(\"FailureReasonsJson\") = 'array'"));

            builder.HasKey(e => e.Id);

            builder.Property(e => e.FailureReasonsJson)
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(e => e.EvaluatedAt).HasDefaultValueSql("now()");

            builder.HasOne(x => x.Application)
                .WithOne(x => x.EligibilityResult)
                .HasForeignKey<EligibilityResult>(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

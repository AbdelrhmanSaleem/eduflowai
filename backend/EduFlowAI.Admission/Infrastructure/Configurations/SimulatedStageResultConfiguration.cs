using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class SimulatedStageResultConfiguration : IEntityTypeConfiguration<SimulatedStageResult>
    {
        public void Configure(EntityTypeBuilder<SimulatedStageResult> builder)
        {
            builder.ToTable("SimulatedStageResults", schema: "admissions", t =>
            {
                t.HasCheckConstraint("CK_SimulatedStageResult_Stage",
                    "\"Stage\" IN ('EnglishExamAndIq','ProgrammingExam','TechnicalInterview','SoftSkillsInterview')");
                t.HasCheckConstraint("CK_SimulatedStageResult_Result",
                    "\"Result\" IN ('Pending','Passed','NotPassed')");
                t.HasCheckConstraint("CK_SimulatedStageResult_Score_Range",
                    "\"Score\" IS NULL OR (\"Score\" >= 0 AND \"Score\" <= 100)");
            });

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Stage).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(s => s.Result).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(s => s.Score).HasColumnType("decimal(5,2)");
            builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

            builder.HasIndex(s => new { s.ApplicationId, s.Stage, s.TrackId }).IsUnique();

            builder.HasOne(s => s.Application)
                .WithMany(a => a.SimulatedStageResults)
                .HasForeignKey(s => s.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

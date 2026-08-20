using EduFlowAI.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Identity.Infrastructure.Configurations
{
    public class ApplicantProfileConfiguration : IEntityTypeConfiguration<ApplicantProfile>
    {
        public void Configure(EntityTypeBuilder<ApplicantProfile> builder)
        {
            builder.ToTable("ApplicantProfiles", "identity");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("now()")
                    .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(x => x.IsProfileLocked)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            builder.Property(x => x.GraduationYear)
                .IsRequired();
            // String lengths and requirements
            builder.Property(x => x.FullNameEn).HasMaxLength(300).IsRequired();
            builder.Property(x => x.FullNameAr).HasMaxLength(300).IsRequired();
            builder.Property(x => x.NationalId).HasMaxLength(14).IsRequired();
            builder.Property(x => x.Nationality).HasMaxLength(5).IsRequired();
            builder.Property(x => x.University).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Faculty).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Major).HasMaxLength(200).IsRequired();
            builder.Property(x => x.DegreeLevel).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Governorate).HasMaxLength(100);

            // Enum Conversions: Save Enums as Strings in PostgreSQL
            builder.Property(x => x.Gender).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(x => x.CumulativeGrade).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(x => x.MilitaryStatus).HasConversion<string>().HasMaxLength(30);

            // Unique Indexes
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => x.NationalId).IsUnique();

            // PostgreSQL CHECK Constraints (The Iron-Clad Rules)
            // 1. National ID must be exactly 14 digits
            builder.ToTable(t => t.HasCheckConstraint("CK_ApplicantProfile_NationalId", "\"NationalId\" ~ '^[0-9]{14}$'"));

            // 2. Gender and Grade strings must match the allowed values
            builder.ToTable(t => t.HasCheckConstraint("CK_ApplicantProfile_Gender", "\"Gender\" IN ('Male', 'Female')"));
            builder.ToTable(t => t.HasCheckConstraint("CK_ApplicantProfile_CumulativeGrade", "\"CumulativeGrade\" IN ('Acceptable', 'Good', 'VeryGood', 'Excellent')"));

            // 3. Conditional Military Status logic (NULL for Females, required valid status for Males)
            builder.ToTable(t => t.HasCheckConstraint("CK_ApplicantProfile_MilitaryStatus",
                "(\"Gender\" = 'Female' AND \"MilitaryStatus\" IS NULL) OR " +
                "(\"Gender\" = 'Male' AND \"MilitaryStatus\" IN ('Completed', 'Exempted', 'Postponed', 'CurrentlyServing'))"));

            // Relationships (1-to-1 with AppUser)
            builder.HasOne(x => x.User)
                .WithOne(x => x.ApplicantProfile)
                .HasForeignKey<ApplicantProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

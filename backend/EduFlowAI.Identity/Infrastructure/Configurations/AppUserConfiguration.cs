using EduFlowAI.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Reflection.Emit;

namespace EduFlowAI.Identity.Infrastructure.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("AspNetUsers", "public");

            builder.Property(x => x.PreferredLanguage)
                .HasMaxLength(5)
                .HasDefaultValue("en")
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.GmailNotificationsEnabled)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_AppUser_PreferredLanguage",
                "\"PreferredLanguage\" IN ('en', 'ar')"));
        }
    }
}

using EduFlowAI.Communication.Domain.Entities;
using EduFlowAI.Communication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Communication.Infrastructure.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notification", schema: "communication", t =>
                t.HasCheckConstraint("CK_Notification_GmailStatus",
                    "\"GmailStatus\" IN ('NotRequested','Pending','Sent','Failed')"));

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(n => n.Message).HasColumnType("text").IsRequired();
            builder.Property(n => n.GmailStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                //.HasDefaultValue(GmailStatus.NotRequested)
                .IsRequired();
            builder.Property(n => n.CreatedAt).HasDefaultValueSql("now()");
            builder.Property(x => x.IsRead)
                .HasDefaultValue(false)
                .IsRequired();

            builder.HasIndex(x => new
            {
                x.UserId,
                x.IsRead,
                x.CreatedAt
            });
            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.ApplicationId);
        }
    }
}

using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class TrackConfiguration : IEntityTypeConfiguration<Track>
    {
        public void Configure(EntityTypeBuilder<Track> builder)
        {
            builder.ToTable("Tracks", "admissions", table =>
            {
                table.HasCheckConstraint(
                    "CK_Track_PrerequisiteTopics_JsonArray",
                    "jsonb_typeof(\"PrerequisiteTopics\") = 'array'");
            });
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            var prerequisiteTopicsComparer = new ValueComparer<List<string>>(
                (left, right) =>
                    ReferenceEquals(left, right) ||
                    (left != null && right != null && left.SequenceEqual(right)),
                topics => topics.Aggregate(
                    0,
                    (hash, topic) => HashCode.Combine(
                        hash,
                        topic == null
                            ? 0
                            : StringComparer.Ordinal.GetHashCode(topic))),
                topics => topics.ToList());

            var prerequisiteTopicsProperty = builder.Property(x => x.PrerequisiteTopics)
                .HasConversion(
                    topics => JsonSerializer.Serialize(
                        topics,
                        (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(
                                json,
                                (JsonSerializerOptions?)null) ??
                            new List<string>())
                .HasColumnType("jsonb")
                .IsRequired();

            prerequisiteTopicsProperty.Metadata.SetValueComparer(prerequisiteTopicsComparer);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasIndex(x => new { x.ProgramId, x.Name })
                .HasDatabaseName("UX_Tracks_Program_Name")
                .IsUnique();

            builder.HasOne(x => x.Program)
                .WithMany(x => x.Tracks)
                .HasForeignKey(x => x.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

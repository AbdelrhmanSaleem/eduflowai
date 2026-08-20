using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduFlowAI.Admission.Infrastructure.Configurations
{
    public class TrackBranchOfferingConfiguration : IEntityTypeConfiguration<TrackBranchOffering>
    {
        public void Configure(EntityTypeBuilder<TrackBranchOffering> builder)
        {
            builder.ToTable("TrackBranchOfferings", "admissions");
            builder.HasKey(x => x.Id);

            builder.ToTable(t => t.HasCheckConstraint("CK_TrackBranchOffering_Capacity", "\"Capacity\" > 0"));

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasIndex(x => new { x.CycleId, x.TrackId, x.BranchId })
                .HasDatabaseName("UX_TrackBranchOfferings_Cycle_Track_Branch")
                .IsUnique();

            builder.HasOne(x => x.Cycle)
                .WithMany(x => x.Offerings)
                .HasForeignKey(x => x.CycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Track)
                .WithMany(x => x.BranchOfferings)
                .HasForeignKey(x => x.TrackId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Branch)
                .WithMany(x => x.BranchOfferings)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class RankingSnapshotConfiguration : IEntityTypeConfiguration<RankingSnapshot>
{
    public void Configure(EntityTypeBuilder<RankingSnapshot> builder)
    {
        builder.ToTable("RankingSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Year)
            .IsRequired();

        builder.Property(x => x.CachedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.Circuit)
            .WithMany()
            .HasForeignKey(x => x.CircuitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Entries)
            .WithOne(x => x.RankingSnapshot)
            .HasForeignKey(x => x.RankingSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CircuitId, x.CategoryId, x.Year }).IsUnique();
        builder.HasIndex(x => new { x.CategoryId, x.Year, x.CachedAtUtc });
    }
}

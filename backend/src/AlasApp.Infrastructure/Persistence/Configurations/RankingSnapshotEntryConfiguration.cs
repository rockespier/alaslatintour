using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class RankingSnapshotEntryConfiguration : IEntityTypeConfiguration<RankingSnapshotEntry>
{
    public void Configure(EntityTypeBuilder<RankingSnapshotEntry> builder)
    {
        builder.ToTable("RankingSnapshotEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompetitorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Position).IsRequired();
        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.Events).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.RankingSnapshotId, x.Position }).IsUnique();
    }
}

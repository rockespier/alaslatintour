using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class EventResultConfiguration : IEntityTypeConfiguration<EventResult>
{
    public void Configure(EntityTypeBuilder<EventResult> builder)
    {
        builder.ToTable("EventResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Place)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PrizeUsd).HasPrecision(18, 2);
        builder.Property(x => x.HeatOla1).HasPrecision(6, 2);
        builder.Property(x => x.HeatOla2).HasPrecision(6, 2);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.Event)
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Competitor)
            .WithMany()
            .HasForeignKey(x => x.CompetitorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EventId, x.CategoryId, x.CompetitorId }).IsUnique();
        builder.HasIndex(x => new { x.EventId, x.CategoryId, x.Place });
    }
}

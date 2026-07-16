using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class CompetitorFineConfiguration : IEntityTypeConfiguration<CompetitorFine>
{
    public void Configure(EntityTypeBuilder<CompetitorFine> builder)
    {
        builder.ToTable("CompetitorFines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AmountUsd)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne<Competitor>()
            .WithMany(x => x.Fines)
            .HasForeignKey(x => x.CompetitorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CompetitorId);
        builder.HasIndex(x => x.Status);
    }
}

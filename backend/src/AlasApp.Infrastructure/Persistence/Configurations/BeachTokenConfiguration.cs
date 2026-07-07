using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class BeachTokenConfiguration : IEntityTypeConfiguration<BeachToken>
{
    public void Configure(EntityTypeBuilder<BeachToken> builder)
    {
        builder.ToTable("BeachTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenCode)
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.Inscription)
            .WithMany()
            .HasForeignKey(x => x.InscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TokenCode).IsUnique().HasFilter("[TokenCode] IS NOT NULL");
        builder.HasIndex(x => new { x.InscriptionId, x.Status });
    }
}

using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.AmountUsd)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.Fecha).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.Inscription)
            .WithMany()
            .HasForeignKey(x => x.InscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.InscriptionId).IsUnique();
        builder.HasIndex(x => x.TransactionId).IsUnique();
        builder.HasIndex(x => new { x.Method, x.Status, x.Fecha });
    }
}

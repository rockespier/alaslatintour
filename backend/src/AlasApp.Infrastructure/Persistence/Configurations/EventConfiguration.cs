using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Pais)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Ciudad)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Playa)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ImagenUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.SurfScoresCode)
            .HasMaxLength(100);

        builder.Property(x => x.AccessType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Estado)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PrizeAmountUsd)
            .HasPrecision(18, 2);

        builder.Property(x => x.UseCircuitTariffs)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.CircuitId);
        builder.HasIndex(x => x.Estado);
        builder.HasIndex(x => x.Pais);
        builder.HasIndex(x => x.FechaInicio);
    }
}

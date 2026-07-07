using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class CompetitorConfiguration : IEntityTypeConfiguration<Competitor>
{
    public void Configure(EntityTypeBuilder<Competitor> builder)
    {
        builder.ToTable("Competitors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Apellido)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Pais)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Telefono)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Club)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NumeroCamiseta)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Patrocinadores)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Federacion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SurfScoresCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LicenseNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LicenseNumberLong)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Genero)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Postura)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TallaCamiseta)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.LicenseStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NotificationEmail).IsRequired();
        builder.Property(x => x.NotificationPush).IsRequired();
        builder.Property(x => x.NotificationResultados).IsRequired();
        builder.Property(x => x.NotificationInscripciones).IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasMany(x => x.EnabledLicenseCategories)
            .WithOne(x => x.Competitor)
            .HasForeignKey(x => x.CompetitorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Pais);
        builder.HasIndex(x => x.LicenseStatus);
    }
}

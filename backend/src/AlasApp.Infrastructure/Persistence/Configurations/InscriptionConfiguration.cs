using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class InscriptionConfiguration : IEntityTypeConfiguration<Inscription>
{
    public void Configure(EntityTypeBuilder<Inscription> builder)
    {
        builder.ToTable("Inscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShirtNumber)
            .HasMaxLength(20);

        builder.Property(x => x.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.MontoUsd)
            .HasPrecision(18, 2);

        builder.Property(x => x.BaseAmountUsd)
            .HasPrecision(18, 2);

        builder.Property(x => x.AdministrativeFeeUsd)
            .HasPrecision(18, 2);

        builder.Property(x => x.EstadoAdmin)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EstadoCompetidor)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Resultado)
            .HasMaxLength(250);

        builder.Property(x => x.TransaccionId)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.ReglamentoAceptado).IsRequired();
        builder.Property(x => x.RiesgosAceptados).IsRequired();
        builder.Property(x => x.UsoImagenAceptado).IsRequired();
        builder.Property(x => x.InscripcionAt).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne(x => x.Competitor)
            .WithMany()
            .HasForeignKey(x => x.CompetitorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Event)
            .WithMany()
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompetitorId, x.EventId, x.CategoryId }).IsUnique();
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.EstadoAdmin);
    }
}

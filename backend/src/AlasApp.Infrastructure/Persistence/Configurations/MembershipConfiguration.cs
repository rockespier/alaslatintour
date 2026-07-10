using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClubFederacion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Pais)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Plan)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.InicioVigencia).IsRequired();
        builder.Property(x => x.Vencimiento).IsRequired();

        builder.Property(x => x.EmailContacto)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.Pais, x.ClubFederacion });
        builder.HasIndex(x => x.Vencimiento);
    }
}

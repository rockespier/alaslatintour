using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Apellido)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Pais)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IdiomaPreferido)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AdminRole)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.TokenVersion).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Newsletter).IsRequired();
        builder.Property(x => x.AcceptedTerms).IsRequired();
        builder.Property(x => x.AcceptedReglamento).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasOne<Competitor>()
            .WithOne()
            .HasForeignKey<UserAccount>(x => x.CompetitorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.CompetitorId).IsUnique().HasFilter("[CompetitorId] IS NOT NULL");
    }
}

using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class CompetitorLicenseCategoryConfiguration : IEntityTypeConfiguration<CompetitorLicenseCategory>
{
    public void Configure(EntityTypeBuilder<CompetitorLicenseCategory> builder)
    {
        builder.ToTable("CompetitorLicenseCategories");

        builder.HasKey(x => new { x.CompetitorId, x.CategoryId });

        builder.Property(x => x.CategoryId)
            .HasMaxLength(100)
            .IsRequired();
    }
}

using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlasApp.Infrastructure.Persistence.Configurations;

public sealed class CategoryTariffConfiguration : IEntityTypeConfiguration<CategoryTariff>
{
    public void Configure(EntityTypeBuilder<CategoryTariff> builder)
    {
        builder.ToTable("CategoryTariffs");

        builder.HasKey(x => new { x.CategoryId, x.StarLevel });

        builder.Property(x => x.Usd).HasPrecision(18, 2);
        builder.Property(x => x.Cop).HasPrecision(18, 2);
    }
}

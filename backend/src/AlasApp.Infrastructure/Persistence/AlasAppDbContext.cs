using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence;

public sealed class AlasAppDbContext(DbContextOptions<AlasAppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Circuit> Circuits => Set<Circuit>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CategoryTariff> CategoryTariffs => Set<CategoryTariff>();

    public DbSet<EventCategory> EventCategories => Set<EventCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlasAppDbContext).Assembly);
    }
}

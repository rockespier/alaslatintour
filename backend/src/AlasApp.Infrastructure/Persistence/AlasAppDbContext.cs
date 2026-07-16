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

    public DbSet<Competitor> Competitors => Set<Competitor>();

    public DbSet<CompetitorFine> CompetitorFines => Set<CompetitorFine>();

    public DbSet<CompetitorLicenseCategory> CompetitorLicenseCategories => Set<CompetitorLicenseCategory>();

    public DbSet<Inscription> Inscriptions => Set<Inscription>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<BeachToken> BeachTokens => Set<BeachToken>();

    public DbSet<RankingSnapshot> RankingSnapshots => Set<RankingSnapshot>();

    public DbSet<RankingSnapshotEntry> RankingSnapshotEntries => Set<RankingSnapshotEntry>();

    public DbSet<EventResult> EventResults => Set<EventResult>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlasAppDbContext).Assembly);
    }
}

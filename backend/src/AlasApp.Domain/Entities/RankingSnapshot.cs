using AlasApp.Domain.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class RankingSnapshot : AuditableEntity
{
    private readonly List<RankingSnapshotEntry> _entries = [];

    private RankingSnapshot()
    {
    }

    private RankingSnapshot(
        Guid id,
        Guid circuitId,
        Guid categoryId,
        string categoryName,
        int year,
        DateTimeOffset cachedAtUtc)
    {
        Id = id;
        CircuitId = circuitId;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Year = year;
        CachedAtUtc = cachedAtUtc;
    }

    public Guid CircuitId { get; private set; }

    public Circuit? Circuit { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public string CategoryName { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public DateTimeOffset CachedAtUtc { get; private set; }

    public IReadOnlyCollection<RankingSnapshotEntry> Entries => _entries;

    public static RankingSnapshot Create(
        Guid circuitId,
        Guid categoryId,
        string categoryName,
        int year,
        DateTimeOffset cachedAtUtc)
    {
        Validate(circuitId, categoryId, categoryName, year);

        return new RankingSnapshot(
            Guid.NewGuid(),
            circuitId,
            categoryId,
            categoryName.Trim(),
            year,
            cachedAtUtc);
    }

    public void AddEntry(
        string competitorName,
        string country,
        int position,
        int points,
        int events,
        int? variation)
    {
        _entries.Add(RankingSnapshotEntry.Create(Id, competitorName, country, position, points, events, variation));
    }

    private static void Validate(Guid circuitId, Guid categoryId, string categoryName, int year)
    {
        if (circuitId == Guid.Empty || categoryId == Guid.Empty)
        {
            throw new DomainRuleException("Circuito y categoria son obligatorios para el ranking.");
        }

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new DomainRuleException("El nombre de la categoria del ranking es obligatorio.");
        }

        if (year is < 2020 or > 2035)
        {
            throw new DomainRuleException("El anio del ranking debe estar entre 2020 y 2035.");
        }
    }
}

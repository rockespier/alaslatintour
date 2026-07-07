using AlasApp.Domain.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class RankingSnapshotEntry : AuditableEntity
{
    private RankingSnapshotEntry()
    {
    }

    private RankingSnapshotEntry(
        Guid id,
        Guid rankingSnapshotId,
        string competitorName,
        string country,
        int position,
        int points,
        int events,
        int? variation)
    {
        Id = id;
        RankingSnapshotId = rankingSnapshotId;
        CompetitorName = competitorName;
        Country = country;
        Position = position;
        Points = points;
        Events = events;
        Variation = variation;
    }

    public Guid RankingSnapshotId { get; private set; }

    public RankingSnapshot? RankingSnapshot { get; private set; }

    public string CompetitorName { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public int Points { get; private set; }

    public int Events { get; private set; }

    public int? Variation { get; private set; }

    public static RankingSnapshotEntry Create(
        Guid rankingSnapshotId,
        string competitorName,
        string country,
        int position,
        int points,
        int events,
        int? variation)
    {
        Validate(rankingSnapshotId, competitorName, country, position, points, events);

        return new RankingSnapshotEntry(
            Guid.NewGuid(),
            rankingSnapshotId,
            competitorName.Trim(),
            country.Trim(),
            position,
            points,
            events,
            variation);
    }

    private static void Validate(
        Guid rankingSnapshotId,
        string competitorName,
        string country,
        int position,
        int points,
        int events)
    {
        if (rankingSnapshotId == Guid.Empty)
        {
            throw new DomainRuleException("El snapshot del ranking es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(competitorName))
        {
            throw new DomainRuleException("El nombre del competidor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainRuleException("El pais del competidor es obligatorio.");
        }

        if (position <= 0)
        {
            throw new DomainRuleException("La posicion del ranking debe ser mayor que cero.");
        }

        if (points < 0 || events < 0)
        {
            throw new DomainRuleException("Puntos y eventos no pueden ser negativos.");
        }
    }
}

using AlasApp.Domain.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class EventResult : AuditableEntity
{
    private EventResult()
    {
    }

    private EventResult(
        Guid id,
        Guid eventId,
        Guid categoryId,
        Guid competitorId,
        string place,
        int ligaPoints,
        decimal? prizeUsd,
        decimal? heatOla1,
        decimal? heatOla2,
        DateTimeOffset timestamp)
    {
        Id = id;
        EventId = eventId;
        CategoryId = categoryId;
        CompetitorId = competitorId;
        Place = place;
        LigaPoints = ligaPoints;
        PrizeUsd = prizeUsd;
        HeatOla1 = heatOla1;
        HeatOla2 = heatOla2;
        SetCreated(timestamp);
    }

    public Guid EventId { get; private set; }

    public Event? Event { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public Guid CompetitorId { get; private set; }

    public Competitor? Competitor { get; private set; }

    public string Place { get; private set; } = string.Empty;

    public int LigaPoints { get; private set; }

    public decimal? PrizeUsd { get; private set; }

    public decimal? HeatOla1 { get; private set; }

    public decimal? HeatOla2 { get; private set; }

    public decimal? HeatScoreTotal => HeatOla1.HasValue || HeatOla2.HasValue
        ? (HeatOla1 ?? 0) + (HeatOla2 ?? 0)
        : null;

    public static EventResult Create(
        Guid eventId,
        Guid categoryId,
        Guid competitorId,
        string place,
        int ligaPoints,
        decimal? prizeUsd,
        decimal? heatOla1,
        decimal? heatOla2,
        DateTimeOffset timestamp)
    {
        Validate(eventId, categoryId, competitorId, place, ligaPoints, prizeUsd, heatOla1, heatOla2);

        return new EventResult(
            Guid.NewGuid(),
            eventId,
            categoryId,
            competitorId,
            place.Trim(),
            ligaPoints,
            prizeUsd,
            heatOla1,
            heatOla2,
            timestamp);
    }

    public void Update(
        string place,
        int ligaPoints,
        decimal? prizeUsd,
        decimal? heatOla1,
        decimal? heatOla2,
        DateTimeOffset timestamp)
    {
        Validate(EventId, CategoryId, CompetitorId, place, ligaPoints, prizeUsd, heatOla1, heatOla2);

        Place = place.Trim();
        LigaPoints = ligaPoints;
        PrizeUsd = prizeUsd;
        HeatOla1 = heatOla1;
        HeatOla2 = heatOla2;
        SetUpdated(timestamp);
    }

    private static void Validate(
        Guid eventId,
        Guid categoryId,
        Guid competitorId,
        string place,
        int ligaPoints,
        decimal? prizeUsd,
        decimal? heatOla1,
        decimal? heatOla2)
    {
        if (eventId == Guid.Empty || categoryId == Guid.Empty || competitorId == Guid.Empty)
        {
            throw new DomainRuleException("Evento, categoria y competidor son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(place))
        {
            throw new DomainRuleException("El puesto del resultado es obligatorio.");
        }

        if (place.Trim().Length > 30)
        {
            throw new DomainRuleException("El puesto del resultado no puede exceder 30 caracteres.");
        }

        if (ligaPoints < 0)
        {
            throw new DomainRuleException("Los puntos de liga no pueden ser negativos.");
        }

        if (prizeUsd < 0 || heatOla1 < 0 || heatOla2 < 0)
        {
            throw new DomainRuleException("Los valores numericos del resultado no pueden ser negativos.");
        }
    }
}

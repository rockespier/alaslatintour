using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class EventCategory
{
    private EventCategory()
    {
    }

    private EventCategory(
        Guid eventId,
        Guid categoryId,
        int? stars,
        decimal? customTariffUsd,
        int? capacidad)
    {
        EventId = eventId;
        CategoryId = categoryId;
        Stars = stars;
        CustomTariffUsd = customTariffUsd;
        Capacidad = capacidad;
    }

    public Guid EventId { get; private set; }

    public Event? Event { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    /// <summary>
    /// Nivel de estrellas propio de esta categoria en el evento. Si es null, se usa el nivel de estrellas del evento (Event.Stars).
    /// </summary>
    public int? Stars { get; private set; }

    public decimal? CustomTariffUsd { get; private set; }

    public int? Capacidad { get; private set; }

    public static EventCategory Create(
        Guid eventId,
        Guid categoryId,
        int? stars,
        decimal? customTariffUsd,
        int? capacidad)
    {
        Validate(eventId, categoryId, stars, customTariffUsd, capacidad);
        return new EventCategory(eventId, categoryId, stars, customTariffUsd, capacidad);
    }

    public void Update(int? stars, decimal? customTariffUsd, int? capacidad)
    {
        Validate(EventId, CategoryId, stars, customTariffUsd, capacidad);
        Stars = stars;
        CustomTariffUsd = customTariffUsd;
        Capacidad = capacidad;
    }

    private static void Validate(
        Guid eventId,
        Guid categoryId,
        int? stars,
        decimal? customTariffUsd,
        int? capacidad)
    {
        if (eventId == Guid.Empty || categoryId == Guid.Empty)
        {
            throw new DomainRuleException("El evento y la categoria son obligatorios.");
        }

        if (stars.HasValue && stars.Value is < 1 or > 7)
        {
            throw new DomainRuleException("El nivel de estrellas de la categoria del evento debe estar entre 1 y 7.");
        }

        if (customTariffUsd.HasValue && customTariffUsd.Value < 0)
        {
            throw new DomainRuleException("La tarifa personalizada en USD no puede ser negativa.");
        }

        if (capacidad.HasValue && capacidad.Value < 0)
        {
            throw new DomainRuleException("La capacidad de la categoria del evento no puede ser negativa.");
        }
    }
}

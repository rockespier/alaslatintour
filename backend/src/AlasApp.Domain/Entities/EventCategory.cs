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
        decimal? customTariffUsd,
        decimal? customTariffCop,
        int? capacidad)
    {
        EventId = eventId;
        CategoryId = categoryId;
        CustomTariffUsd = customTariffUsd;
        CustomTariffCop = customTariffCop;
        Capacidad = capacidad;
    }

    public Guid EventId { get; private set; }

    public Event? Event { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public decimal? CustomTariffUsd { get; private set; }

    public decimal? CustomTariffCop { get; private set; }

    public int? Capacidad { get; private set; }

    public static EventCategory Create(
        Guid eventId,
        Guid categoryId,
        decimal? customTariffUsd,
        decimal? customTariffCop,
        int? capacidad)
    {
        Validate(eventId, categoryId, customTariffUsd, customTariffCop, capacidad);
        return new EventCategory(eventId, categoryId, customTariffUsd, customTariffCop, capacidad);
    }

    public void Update(decimal? customTariffUsd, decimal? customTariffCop, int? capacidad)
    {
        Validate(EventId, CategoryId, customTariffUsd, customTariffCop, capacidad);
        CustomTariffUsd = customTariffUsd;
        CustomTariffCop = customTariffCop;
        Capacidad = capacidad;
    }

    private static void Validate(
        Guid eventId,
        Guid categoryId,
        decimal? customTariffUsd,
        decimal? customTariffCop,
        int? capacidad)
    {
        if (eventId == Guid.Empty || categoryId == Guid.Empty)
        {
            throw new DomainRuleException("El evento y la categoria son obligatorios.");
        }

        if (customTariffUsd.HasValue && customTariffUsd.Value < 0)
        {
            throw new DomainRuleException("La tarifa personalizada en USD no puede ser negativa.");
        }

        if (customTariffCop.HasValue && customTariffCop.Value < 0)
        {
            throw new DomainRuleException("La tarifa personalizada en COP no puede ser negativa.");
        }

        if (capacidad.HasValue && capacidad.Value < 0)
        {
            throw new DomainRuleException("La capacidad de la categoria del evento no puede ser negativa.");
        }
    }
}

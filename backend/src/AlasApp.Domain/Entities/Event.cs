using AlasApp.Domain.Common;
using AlasApp.Domain.Enums;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Domain.Entities;

public sealed class Event : AuditableEntity
{
    private readonly List<EventCategory> _categories = [];

    private Event()
    {
    }

    private Event(
        Guid id,
        Guid circuitId,
        string nombre,
        DateTimeOffset fechaInicio,
        DateTimeOffset fechaFin,
        string pais,
        string ciudad,
        string playa,
        string? auspiciador,
        int stars,
        int capacidadMaxima,
        decimal prizeAmountUsd,
        string? imagenUrl,
        string? surfScoresCode,
        EventType eventType,
        EventAccessType accessType,
        EventStatusAdmin estado,
        bool useCircuitTariffs)
    {
        Id = id;
        CircuitId = circuitId;
        Nombre = nombre;
        FechaInicio = NormalizeDate(fechaInicio);
        FechaFin = NormalizeDate(fechaFin);
        Pais = pais;
        Ciudad = ciudad;
        Playa = playa;
        Auspiciador = auspiciador;
        Stars = stars;
        CapacidadMaxima = capacidadMaxima;
        PrizeAmountUsd = prizeAmountUsd;
        ImagenUrl = imagenUrl;
        SurfScoresCode = surfScoresCode;
        EventType = eventType;
        AccessType = accessType;
        Estado = estado;
        UseCircuitTariffs = useCircuitTariffs;
    }

    public Guid CircuitId { get; private set; }

    public Circuit? Circuit { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public DateTimeOffset FechaInicio { get; private set; }

    public DateTimeOffset FechaFin { get; private set; }

    public string Pais { get; private set; } = string.Empty;

    public string Ciudad { get; private set; } = string.Empty;

    public string Playa { get; private set; } = string.Empty;

    public string? Auspiciador { get; private set; }

    public int Stars { get; private set; }

    public int CapacidadMaxima { get; private set; }

    public decimal PrizeAmountUsd { get; private set; }

    public string? ImagenUrl { get; private set; }

    public string? SurfScoresCode { get; private set; }

    public EventType EventType { get; private set; }

    public EventAccessType AccessType { get; private set; }

    public EventStatusAdmin Estado { get; private set; }

    public bool UseCircuitTariffs { get; private set; }

    public IReadOnlyCollection<EventCategory> Categories => _categories;

    public static Event Create(
        Guid circuitId,
        string nombre,
        DateTimeOffset fechaInicio,
        DateTimeOffset fechaFin,
        string pais,
        string ciudad,
        string playa,
        string? auspiciador,
        int stars,
        int capacidadMaxima,
        decimal prizeAmountUsd,
        string? imagenUrl,
        string? surfScoresCode,
        EventType eventType,
        EventAccessType accessType,
        EventStatusAdmin estado)
    {
        Validate(circuitId, nombre, fechaInicio, fechaFin, pais, ciudad, playa, auspiciador, stars, capacidadMaxima, prizeAmountUsd, imagenUrl, surfScoresCode);

        return new Event(
            Guid.NewGuid(),
            circuitId,
            nombre.Trim(),
            fechaInicio,
            fechaFin,
            pais.Trim(),
            ciudad.Trim(),
            playa.Trim(),
            NormalizeOptional(auspiciador),
            stars,
            capacidadMaxima,
            prizeAmountUsd,
            NormalizeOptional(imagenUrl),
            NormalizeOptional(surfScoresCode),
            eventType,
            accessType,
            estado,
            true);
    }

    public void Update(
        Guid circuitId,
        string nombre,
        DateTimeOffset fechaInicio,
        DateTimeOffset fechaFin,
        string pais,
        string ciudad,
        string playa,
        string? auspiciador,
        int stars,
        int capacidadMaxima,
        decimal prizeAmountUsd,
        string? imagenUrl,
        string? surfScoresCode,
        EventType eventType,
        EventAccessType accessType,
        EventStatusAdmin estado)
    {
        Validate(circuitId, nombre, fechaInicio, fechaFin, pais, ciudad, playa, auspiciador, stars, capacidadMaxima, prizeAmountUsd, imagenUrl, surfScoresCode);

        CircuitId = circuitId;
        Nombre = nombre.Trim();
        FechaInicio = NormalizeDate(fechaInicio);
        FechaFin = NormalizeDate(fechaFin);
        Pais = pais.Trim();
        Ciudad = ciudad.Trim();
        Playa = playa.Trim();
        Auspiciador = NormalizeOptional(auspiciador);
        Stars = stars;
        CapacidadMaxima = capacidadMaxima;
        PrizeAmountUsd = prizeAmountUsd;
        ImagenUrl = NormalizeOptional(imagenUrl);
        SurfScoresCode = NormalizeOptional(surfScoresCode);
        EventType = eventType;
        AccessType = accessType;
        Estado = estado;
    }

    public int ApplyRankingBonus(int basePoints)
    {
        if (basePoints < 0)
        {
            throw new DomainRuleException("Los puntos base no pueden ser negativos.");
        }

        var multiplier = EventType switch
        {
            EventType.Prime => 1.10m,
            EventType.SuperPrime => 1.50m,
            _ => 1.00m
        };

        return (int)Math.Round(basePoints * multiplier, MidpointRounding.AwayFromZero);
    }

    public void ReplaceCategories(IEnumerable<EventCategory> categories, bool useCircuitTariffs)
    {
        _categories.Clear();
        _categories.AddRange(categories);
        UseCircuitTariffs = useCircuitTariffs;
    }

    public EventStatusPublic GetPublicStatus()
    {
        return Estado switch
        {
            EventStatusAdmin.Activo => EventStatusPublic.InscripcionesAbiertas,
            EventStatusAdmin.Proximo => EventStatusPublic.Proximamente,
            EventStatusAdmin.Borrador => EventStatusPublic.Proximamente,
            EventStatusAdmin.Completado => EventStatusPublic.Completado,
            EventStatusAdmin.Cancelado => EventStatusPublic.Cerrado,
            _ => EventStatusPublic.Cerrado
        };
    }

    public string GetLugar()
    {
        return $"{Ciudad}, {Pais}";
    }

    private static void Validate(
        Guid circuitId,
        string nombre,
        DateTimeOffset fechaInicio,
        DateTimeOffset fechaFin,
        string pais,
        string ciudad,
        string playa,
        string? auspiciador,
        int stars,
        int capacidadMaxima,
        decimal prizeAmountUsd,
        string? imagenUrl,
        string? surfScoresCode)
    {
        if (circuitId == Guid.Empty)
        {
            throw new DomainRuleException("El circuito del evento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainRuleException("El nombre del evento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(pais) || string.IsNullOrWhiteSpace(ciudad) || string.IsNullOrWhiteSpace(playa))
        {
            throw new DomainRuleException("Pais, ciudad y playa son obligatorios.");
        }

        if (auspiciador is not null && auspiciador.Length > 200)
        {
            throw new DomainRuleException("El auspiciador del evento no puede exceder 200 caracteres.");
        }

        if (stars is < 1 or > 7)
        {
            throw new DomainRuleException("El numero de estrellas del evento debe estar entre 1 y 7.");
        }

        if (capacidadMaxima < 0)
        {
            throw new DomainRuleException("La capacidad maxima del evento no puede ser negativa.");
        }

        if (prizeAmountUsd < 0)
        {
            throw new DomainRuleException("El premio del evento no puede ser negativo.");
        }

        if (imagenUrl is not null && imagenUrl.Length > 1000)
        {
            throw new DomainRuleException("La imagen del evento no puede exceder 1000 caracteres.");
        }

        if (NormalizeDate(fechaFin) < NormalizeDate(fechaInicio))
        {
            throw new DomainRuleException("La fecha de fin del evento no puede ser anterior a la fecha de inicio.");
        }

        if (surfScoresCode is not null && surfScoresCode.Length > 100)
        {
            throw new DomainRuleException("El codigo de SurfScores del evento no puede exceder 100 caracteres.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset NormalizeDate(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
    }
}

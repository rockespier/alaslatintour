using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Events.Models;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand(
    Guid EventId,
    Guid CircuitId,
    string Nombre,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    string Pais,
    string Ciudad,
    string Playa,
    int Stars,
    int CapacidadMaxima,
    decimal PrizeAmountUsd,
    string? ImagenUrl,
    string? SurfScoresCode,
    EventAccessType AccessType,
    EventStatusAdmin Estado) : IRequest<EventDto>;

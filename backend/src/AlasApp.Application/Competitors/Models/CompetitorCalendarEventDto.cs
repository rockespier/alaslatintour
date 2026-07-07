namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorCalendarEventDto(
    string EventId,
    string Nombre,
    string Lugar,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    string Categoria,
    string InscriptionStatus,
    int Stars);

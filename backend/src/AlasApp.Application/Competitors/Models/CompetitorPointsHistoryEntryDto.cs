namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorPointsHistoryEntryDto(
    string EventoId,
    string EventoNombre,
    string Ubicacion,
    int Stars,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    string Categoria,
    string Puesto,
    int Puntos,
    bool Cuenta);

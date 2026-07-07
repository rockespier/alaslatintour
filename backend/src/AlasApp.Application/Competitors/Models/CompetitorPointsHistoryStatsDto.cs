namespace AlasApp.Application.Competitors.Models;

public sealed record CompetitorPointsHistoryStatsDto(
    string Posicion,
    int PuntosAcumulados,
    int EventosDisputados,
    int TotalEventos,
    string MejorResultado,
    string MejorResultadoEvento);

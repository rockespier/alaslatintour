namespace AlasApp.Application.Dashboard.Models;

public sealed record DashboardRecentInscriptionDto(
    string CompetitorName,
    string Evento,
    string Categoria,
    DateTimeOffset InscripcionAt);

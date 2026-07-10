namespace AlasApp.Application.Dashboard.Models;

public sealed record DashboardKpiDto(
    int TotalCompetidores,
    int TotalEventosActivos,
    int TotalInscripciones,
    decimal RecaudacionMesUsd,
    int TokensPendientes);

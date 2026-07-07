namespace AlasApp.Application.Competitors.Models;

public sealed record NotificationPreferencesDto(
    bool Email,
    bool Push,
    bool Resultados,
    bool Inscripciones,
    bool Tokens);

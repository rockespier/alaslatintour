namespace AlasApp.Application.Live.Models;

public sealed record PublicLiveStatusDto(
    bool IsLive,
    PublicLiveEventDto? Event,
    string? YouTubeVideoId,
    int YouTubeWidth,
    int YouTubeHeight,
    string? SchedulePdfUrl);

public sealed record PublicLiveEventDto(
    Guid Id,
    string Nombre,
    string Pais,
    string Ciudad,
    string Playa,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    string? ImagenUrl);

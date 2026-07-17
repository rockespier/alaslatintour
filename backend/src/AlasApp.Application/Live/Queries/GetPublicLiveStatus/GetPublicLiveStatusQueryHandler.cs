using System.Text.RegularExpressions;
using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.Live.Models;

namespace AlasApp.Application.Live.Queries.GetPublicLiveStatus;

public sealed partial class GetPublicLiveStatusQueryHandler(
    IAdminSettingsRepository settingsRepository,
    IEventRepository eventRepository)
    : IRequestHandler<GetPublicLiveStatusQuery, PublicLiveStatusDto>
{
    public async Task<PublicLiveStatusDto> Handle(GetPublicLiveStatusQuery request, CancellationToken cancellationToken)
    {
        var json = await settingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(json);
        var youTube = settings.Live.YouTube;
        var schedulePdfUrl = string.IsNullOrWhiteSpace(settings.Live.SchedulePdfUrl) ? null : settings.Live.SchedulePdfUrl;
        var notLive = new PublicLiveStatusDto(false, null, null, youTube.Width, youTube.Height, schedulePdfUrl);

        if (!youTube.Active || youTube.EventId is null)
        {
            return notLive;
        }

        var liveEvent = await eventRepository.GetByIdAsync(youTube.EventId.Value, cancellationToken);
        if (liveEvent is null)
        {
            return notLive;
        }

        return new PublicLiveStatusDto(
            true,
            new PublicLiveEventDto(
                liveEvent.Id,
                liveEvent.Nombre,
                liveEvent.Pais,
                liveEvent.Ciudad,
                liveEvent.Playa,
                liveEvent.FechaInicio,
                liveEvent.FechaFin,
                liveEvent.ImagenUrl),
            ExtractYouTubeVideoId(youTube.VideoIdOrUrl),
            youTube.Width,
            youTube.Height,
            schedulePdfUrl);
    }

    private static string? ExtractYouTubeVideoId(string videoIdOrUrl)
    {
        if (string.IsNullOrWhiteSpace(videoIdOrUrl))
        {
            return null;
        }

        var value = videoIdOrUrl.Trim();
        var match = YouTubeUrlPattern().Match(value);
        if (match.Success)
        {
            return match.Groups["id"].Value;
        }

        return VideoIdPattern().IsMatch(value) ? value : null;
    }

    [GeneratedRegex(@"(?:youtu\.be/|youtube\.com/(?:watch\?v=|embed/|live/|shorts/))(?<id>[A-Za-z0-9_-]{11})")]
    private static partial Regex YouTubeUrlPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoIdPattern();
}

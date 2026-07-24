using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminSettings;
using AlasApp.Application.PublicSiteSettings.Models;

namespace AlasApp.Application.PublicSiteSettings.Queries.GetPublicSiteSettings;

public sealed class GetPublicSiteSettingsQueryHandler(IAdminSettingsRepository settingsRepository)
    : IRequestHandler<GetPublicSiteSettingsQuery, PublicSiteSettingsDto>
{
    public async Task<PublicSiteSettingsDto> Handle(GetPublicSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var json = await settingsRepository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        var settings = AdminSettingsSerializer.DeserializeOrDefault(json);
        return new PublicSiteSettingsDto(settings.General.SocialLinks);
    }
}

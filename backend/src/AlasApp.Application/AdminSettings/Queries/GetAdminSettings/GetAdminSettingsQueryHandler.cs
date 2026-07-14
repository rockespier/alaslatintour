using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.AdminSettings.Queries.GetAdminSettings;

public sealed class GetAdminSettingsQueryHandler(IAdminSettingsRepository repository)
    : IRequestHandler<GetAdminSettingsQuery, AdminSettingsDto>
{
    public async Task<AdminSettingsDto> Handle(GetAdminSettingsQuery request, CancellationToken cancellationToken)
    {
        var json = await repository.GetJsonAsync(AdminSettingsDefaults.SettingsKey, cancellationToken);
        return AdminSettingsSerializer.DeserializeOrDefault(json);
    }
}

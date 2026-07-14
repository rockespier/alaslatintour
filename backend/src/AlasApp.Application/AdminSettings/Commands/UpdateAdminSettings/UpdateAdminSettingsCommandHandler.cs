using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.AdminSettings.Commands.UpdateAdminSettings;

public sealed class UpdateAdminSettingsCommandHandler(
    IAdminSettingsRepository repository,
    IClock clock)
    : IRequestHandler<UpdateAdminSettingsCommand, AdminSettingsDto>
{
    public async Task<AdminSettingsDto> Handle(UpdateAdminSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = AdminSettingsSerializer.Normalize(request.Settings);
        AdminSettingsValidator.Validate(settings);

        var json = AdminSettingsSerializer.Serialize(settings);
        await repository.UpsertJsonAsync(AdminSettingsDefaults.SettingsKey, json, clock.UtcNow, cancellationToken);

        return settings;
    }
}

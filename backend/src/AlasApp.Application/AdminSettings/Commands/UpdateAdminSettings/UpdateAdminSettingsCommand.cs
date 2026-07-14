using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.AdminSettings.Commands.UpdateAdminSettings;

public sealed record UpdateAdminSettingsCommand(AdminSettingsDto Settings) : IRequest<AdminSettingsDto>;

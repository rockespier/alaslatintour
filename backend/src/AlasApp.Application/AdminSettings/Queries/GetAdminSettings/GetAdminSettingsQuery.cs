using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.AdminSettings.Models;

namespace AlasApp.Application.AdminSettings.Queries.GetAdminSettings;

public sealed record GetAdminSettingsQuery : IRequest<AdminSettingsDto>;

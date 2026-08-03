using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.PublicSiteSettings.Models;

namespace AlasApp.Application.PublicSiteSettings.Queries.GetPublicSiteSettings;

public sealed record GetPublicSiteSettingsQuery : IRequest<PublicSiteSettingsDto>;

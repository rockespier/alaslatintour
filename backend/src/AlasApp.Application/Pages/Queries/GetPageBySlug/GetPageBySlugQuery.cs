using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Pages.Models;

namespace AlasApp.Application.Pages.Queries.GetPageBySlug;

public sealed record GetPageBySlugQuery(string Slug, string? Lang = null) : IRequest<PageContentDto>;

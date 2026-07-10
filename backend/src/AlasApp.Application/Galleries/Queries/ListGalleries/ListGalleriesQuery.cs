using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Galleries.Models;

namespace AlasApp.Application.Galleries.Queries.ListGalleries;

public sealed record ListGalleriesQuery : IRequest<IReadOnlyCollection<GallerySummaryDto>>;

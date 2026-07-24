using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.IdentityDocuments;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorIdentityDocument;

public sealed record GetCompetitorIdentityDocumentQuery(Guid CompetitorId) : IRequest<IdentityDocumentDownload>;

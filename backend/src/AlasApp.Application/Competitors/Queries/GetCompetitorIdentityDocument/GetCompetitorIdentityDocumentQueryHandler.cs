using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.IdentityDocuments;

namespace AlasApp.Application.Competitors.Queries.GetCompetitorIdentityDocument;

public sealed class GetCompetitorIdentityDocumentQueryHandler(
    ICompetitorRepository competitorRepository,
    IIdentityDocumentStorage identityDocumentStorage)
    : IRequestHandler<GetCompetitorIdentityDocumentQuery, IdentityDocumentDownload>
{
    public async Task<IdentityDocumentDownload> Handle(GetCompetitorIdentityDocumentQuery request, CancellationToken cancellationToken)
    {
        var competitor = await competitorRepository.GetEntityByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        if (string.IsNullOrWhiteSpace(competitor.IdentityDocumentBlobName))
        {
            throw new NotFoundException("El competidor no tiene un documento de identidad adjunto.");
        }

        return await identityDocumentStorage.DownloadAsync(competitor.IdentityDocumentBlobName, cancellationToken)
            ?? throw new NotFoundException("El documento de identidad no se encontró en el almacenamiento.");
    }
}

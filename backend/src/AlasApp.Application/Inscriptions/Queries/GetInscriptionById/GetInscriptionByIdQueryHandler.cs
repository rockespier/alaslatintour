using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;

namespace AlasApp.Application.Inscriptions.Queries.GetInscriptionById;

public sealed class GetInscriptionByIdQueryHandler(IInscriptionRepository inscriptionRepository)
    : IRequestHandler<GetInscriptionByIdQuery, InscriptionDto>
{
    public async Task<InscriptionDto> Handle(GetInscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        return await inscriptionRepository.GetByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");
    }
}

using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;

namespace AlasApp.Application.Inscriptions.Commands.DeleteInscription;

public sealed class DeleteInscriptionCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteInscriptionCommand, bool>
{
    public async Task<bool> Handle(DeleteInscriptionCommand request, CancellationToken cancellationToken)
    {
        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        inscriptionRepository.Remove(inscription);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

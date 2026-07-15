using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Inscriptions.Commands.DeleteInscription;

public sealed class DeleteInscriptionCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IPaymentRepository paymentRepository,
    IBeachTokenRepository beachTokenRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteInscriptionCommand, bool>
{
    public async Task<bool> Handle(DeleteInscriptionCommand request, CancellationToken cancellationToken)
    {
        if (request.CompetitorId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("No se pudo identificar el competidor autenticado.");
        }

        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        if (inscription.CompetitorId != request.CompetitorId)
        {
            throw new UnauthorizedAccessException("No puedes eliminar una inscripcion de otro competidor.");
        }

        if (!inscription.CanBeDeleted())
        {
            throw new ConflictException("Solo se pueden eliminar inscripciones incompletas y pendientes.");
        }

        if (await paymentRepository.GetEntityByInscriptionIdAsync(request.InscriptionId, cancellationToken) is not null)
        {
            throw new ConflictException("La inscripcion ya tiene un pago registrado y no puede eliminarse.");
        }

        var beachTokens = await beachTokenRepository.ListByInscriptionIdAsync(request.InscriptionId, cancellationToken);
        if (beachTokens.Any(x => x.Status == TokenHistoryStatus.Usado))
        {
            throw new ConflictException("La inscripcion ya tiene un token de playa usado y no puede eliminarse.");
        }

        beachTokenRepository.RemoveRange(beachTokens);
        inscriptionRepository.Remove(inscription);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

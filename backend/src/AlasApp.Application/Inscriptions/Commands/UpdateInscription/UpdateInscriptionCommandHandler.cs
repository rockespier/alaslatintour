using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Inscriptions.Commands.UpdateInscription;

public sealed class UpdateInscriptionCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateInscriptionCommand, InscriptionDto>
{
    public async Task<InscriptionDto> Handle(UpdateInscriptionCommand request, CancellationToken cancellationToken)
    {
        var inscription = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        try
        {
            inscription.Update(request.ShirtNumber, request.EstadoAdmin, request.Notes);
            inscription.SetUpdated(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await inscriptionRepository.GetByIdAsync(inscription.Id, cancellationToken)
                ?? throw new NotFoundException("Inscripcion no encontrada despues de actualizarla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }
}

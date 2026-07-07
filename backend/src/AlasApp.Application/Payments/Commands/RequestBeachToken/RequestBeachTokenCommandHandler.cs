using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Payments.Commands.RequestBeachToken;

public sealed class RequestBeachTokenCommandHandler(
    IInscriptionRepository inscriptionRepository,
    IBeachTokenRepository beachTokenRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<RequestBeachTokenCommand, BeachTokenPendingDto>
{
    public async Task<BeachTokenPendingDto> Handle(RequestBeachTokenCommand request, CancellationToken cancellationToken)
    {
        if (request.InscriptionId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("inscriptionId", "El identificador de la inscripcion es invalido.")]);
        }

        _ = await inscriptionRepository.GetEntityByIdAsync(request.InscriptionId, cancellationToken)
            ?? throw new NotFoundException("Inscripcion no encontrada.");

        if (await beachTokenRepository.HasActiveRequestAsync(request.InscriptionId, clock.UtcNow, cancellationToken))
        {
            throw new ConflictException("Ya existe una solicitud de token activa para esta inscripcion.");
        }

        try
        {
            var beachToken = BeachToken.Create(request.InscriptionId, clock.UtcNow);
            beachToken.SetCreated(clock.UtcNow);

            await beachTokenRepository.AddAsync(beachToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new BeachTokenPendingDto(
                beachToken.Id,
                "pending",
                "Solicitud enviada. El administrador aprobara tu token en breve.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }
}

using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.CompetitorFines.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.CompetitorFines.Commands.CreateCompetitorFine;

public sealed class CreateCompetitorFineCommandHandler(
    ICompetitorRepository competitorRepository,
    ICompetitorFineRepository competitorFineRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateCompetitorFineCommand, CompetitorFineDto>
{
    public async Task<CompetitorFineDto> Handle(CreateCompetitorFineCommand request, CancellationToken cancellationToken)
    {
        if (request.CompetitorId == Guid.Empty || request.CreatedByUserId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [
                    new ValidationError("competitorId", "El competidor es obligatorio."),
                    new ValidationError("createdByUserId", "El usuario administrador es obligatorio.")
                ]);
        }

        _ = await competitorRepository.GetByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        try
        {
            var fine = CompetitorFine.Create(request.CompetitorId, request.AmountUsd, request.Reason, request.Notes, request.CreatedByUserId);
            fine.SetCreated(clock.UtcNow);

            await competitorFineRepository.AddAsync(fine, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await competitorFineRepository.GetByIdAsync(fine.Id, cancellationToken)
                ?? throw new NotFoundException("Multa no encontrada despues de crearla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", [new ValidationError("body", exception.Message)]);
        }
    }
}

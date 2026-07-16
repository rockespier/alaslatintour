using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.CompetitorFines.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.CompetitorFines.Commands.UpdateCompetitorFine;

public sealed class UpdateCompetitorFineCommandHandler(
    ICompetitorFineRepository competitorFineRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateCompetitorFineCommand, CompetitorFineDto>
{
    public async Task<CompetitorFineDto> Handle(UpdateCompetitorFineCommand request, CancellationToken cancellationToken)
    {
        var fine = await competitorFineRepository.GetEntityByIdAsync(request.FineId, cancellationToken)
            ?? throw new NotFoundException("Multa no encontrada.");

        try
        {
            fine.Update(request.AmountUsd, request.Reason, request.Notes, request.Status);
            fine.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await competitorFineRepository.GetByIdAsync(fine.Id, cancellationToken)
                ?? throw new NotFoundException("Multa no encontrada despues de actualizarla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", [new ValidationError("body", exception.Message)]);
        }
    }
}

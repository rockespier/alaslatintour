using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Competitors.Commands.UpdateCompetitor;

public sealed class UpdateCompetitorCommandHandler(
    ICompetitorRepository competitorRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateCompetitorCommand, CompetitorDto>
{
    public async Task<CompetitorDto> Handle(UpdateCompetitorCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var competitor = await competitorRepository.GetEntityByIdAsync(request.CompetitorId, cancellationToken)
            ?? throw new NotFoundException("Competidor no encontrado.");

        if (await competitorRepository.EmailExistsAsync(request.Email, request.CompetitorId, cancellationToken))
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", [new ValidationError("email", "Ya existe un competidor con ese email.")]);
        }

        try
        {
            competitor.Update(
                request.Nombre,
                request.Apellido,
                request.Email,
                request.FechaNacimiento,
                request.Genero,
                request.Pais,
                request.Telefono,
                request.Club,
                request.Postura,
                request.TallaCamiseta,
                request.NumeroCamiseta,
                request.Patrocinadores,
                request.Federacion,
                request.SurfScoresCode);

            competitor.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await competitorRepository.GetByIdAsync(competitor.Id, cancellationToken)
                ?? throw new NotFoundException("Competidor no encontrado despues de actualizarlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(UpdateCompetitorCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.CompetitorId == Guid.Empty)
        {
            errors.Add(new ValidationError("competitorId", "El identificador del competidor es invalido."));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Apellido))
        {
            errors.Add(new ValidationError("apellido", "El apellido es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError("email", "El email es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

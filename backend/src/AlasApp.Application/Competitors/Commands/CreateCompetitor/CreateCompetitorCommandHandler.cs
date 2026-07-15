using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Competitors.Commands.CreateCompetitor;

public sealed class CreateCompetitorCommandHandler(
    ICompetitorRepository competitorRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateCompetitorCommand, CompetitorDto>
{
    public async Task<CompetitorDto> Handle(CreateCompetitorCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        if (await competitorRepository.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", [new ValidationError("email", "Ya existe un competidor con ese email.")]);
        }

        try
        {
            var competitor = Competitor.Create(
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

            competitor.SetCreated(clock.UtcNow);

            await competitorRepository.AddAsync(competitor, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await competitorRepository.GetByIdAsync(competitor.Id, cancellationToken)
                ?? throw new NotFoundException("Competidor no encontrado despues de crearlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(CreateCompetitorCommand request)
    {
        var errors = new List<ValidationError>();

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

        if (string.IsNullOrWhiteSpace(request.Pais))
        {
            errors.Add(new ValidationError("pais", "El pais es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

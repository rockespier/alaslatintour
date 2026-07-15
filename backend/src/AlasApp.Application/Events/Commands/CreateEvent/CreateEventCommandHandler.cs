using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Events.Commands.CreateEvent;

public sealed class CreateEventCommandHandler(
    ICircuitRepository circuitRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        if (await circuitRepository.GetEntityByIdAsync(request.CircuitId, cancellationToken) is null)
        {
            throw new NotFoundException("Circuito no encontrado para el evento.");
        }

        try
        {
            var @event = Event.Create(
                request.CircuitId,
                request.Nombre,
                request.FechaInicio,
                request.FechaFin,
                request.Pais,
                request.Ciudad,
                request.Playa,
                request.Auspiciador,
                request.Stars,
                request.CapacidadMaxima,
                request.PrizeAmountUsd,
                request.ImagenUrl,
                request.SurfScoresCode,
                request.EventType,
                request.AccessType,
                request.Estado);

            @event.SetCreated(clock.UtcNow);

            await eventRepository.AddAsync(@event, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await eventRepository.GetByIdAsync(@event.Id, cancellationToken)
                ?? throw new NotFoundException("Evento no encontrado despues de crearlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(CreateEventCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.CircuitId == Guid.Empty)
        {
            errors.Add(new ValidationError("circuitId", "El circuito es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Pais))
        {
            errors.Add(new ValidationError("pais", "El pais es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Ciudad))
        {
            errors.Add(new ValidationError("ciudad", "La ciudad es obligatoria."));
        }

        if (string.IsNullOrWhiteSpace(request.Playa))
        {
            errors.Add(new ValidationError("playa", "La playa es obligatoria."));
        }

        if (request.Stars is < 1 or > 7)
        {
            errors.Add(new ValidationError("stars", "Las estrellas deben estar entre 1 y 7."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

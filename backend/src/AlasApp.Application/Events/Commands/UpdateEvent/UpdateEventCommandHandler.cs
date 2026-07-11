using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Events.Commands.UpdateEvent;

public sealed class UpdateEventCommandHandler(
    ICircuitRepository circuitRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        if (await circuitRepository.GetEntityByIdAsync(request.CircuitId, cancellationToken) is null)
        {
            throw new NotFoundException("Circuito no encontrado para el evento.");
        }

        var @event = await eventRepository.GetEntityByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado.");

        try
        {
            @event.Update(
                request.CircuitId,
                request.Nombre,
                request.FechaInicio,
                request.FechaFin,
                request.Pais,
                request.Ciudad,
                request.Playa,
                request.Stars,
                request.CapacidadMaxima,
                request.PrizeAmountUsd,
                request.ImagenUrl,
                request.SurfScoresCode,
                request.AccessType,
                request.Estado);

            @event.SetUpdated(clock.UtcNow);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await eventRepository.GetByIdAsync(@event.Id, cancellationToken)
                ?? throw new NotFoundException("Evento no encontrado despues de actualizarlo.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(UpdateEventCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.EventId == Guid.Empty)
        {
            errors.Add(new ValidationError("eventId", "El identificador del evento es invalido."));
        }

        if (request.CircuitId == Guid.Empty)
        {
            errors.Add(new ValidationError("circuitId", "El circuito es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (request.Stars is < 1 or > 5)
        {
            errors.Add(new ValidationError("stars", "Las estrellas deben estar entre 1 y 5."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

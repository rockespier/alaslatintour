using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;

namespace AlasApp.Application.Events.Commands.DeleteEvent;

public sealed class DeleteEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteEventCommand, bool>
{
    public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        if (request.EventId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("eventId", "El identificador del evento es invalido.")]);
        }

        var @event = await eventRepository.GetEntityByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado.");

        eventRepository.Remove(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.EventCategories.Models;

namespace AlasApp.Application.EventCategories.Commands.UpdateEventCategories;

public sealed class UpdateEventCategoriesCommandHandler(
    IEventRepository eventRepository,
    IEventCategoryRepository eventCategoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateEventCategoriesCommand, EventCategoryListDto>
{
    public async Task<EventCategoryListDto> Handle(UpdateEventCategoriesCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var @event = await eventRepository.GetEntityByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado.");

        var assignments = await eventCategoryRepository.BuildAssignmentsAsync(
            request.EventId,
            request.Categories,
            cancellationToken);

        ValidateCategoryCapacityMatchesEventCapacity(@event.CapacidadMaxima, request.Categories);

        @event.ReplaceCategories(assignments, request.UseCircuitTariffs);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await eventCategoryRepository.GetByEventIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Evento no encontrado despues de actualizar sus categorias.");
    }

    private static void Validate(UpdateEventCategoriesCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.EventId == Guid.Empty)
        {
            errors.Add(new ValidationError("eventId", "El identificador del evento es invalido."));
        }

        if (request.Categories.Count != request.Categories.Select(x => x.CategoryId).Distinct().Count())
        {
            errors.Add(new ValidationError("categories", "No se permiten categorias duplicadas en el evento."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }

    private static void ValidateCategoryCapacityMatchesEventCapacity(
        int eventCapacity,
        IReadOnlyCollection<EventCategoryUpsertItem> categories)
    {
        if (categories.Count == 0)
        {
            return;
        }

        var capacityErrors = categories
            .Where(x => !x.Capacidad.HasValue)
            .Select(x => new ValidationError(
                "categories.capacidad",
                $"La capacidad de la categoria {x.CategoryId} es obligatoria para calcular la capacidad total del evento."))
            .ToList();

        if (capacityErrors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", capacityErrors);
        }

        var categoryCapacityTotal = categories.Sum(x => x.Capacidad!.Value);
        if (categoryCapacityTotal != eventCapacity)
        {
            throw new ValidationException(
                "La capacidad total del evento debe ser igual a la suma de las capacidades por categoria.",
                [
                    new ValidationError(
                        "categories.capacidad",
                        $"La suma de capacidades por categoria ({categoryCapacityTotal}) debe ser igual a la capacidad maxima del evento ({eventCapacity}).")
                ]);
        }
    }
}

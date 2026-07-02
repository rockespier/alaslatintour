using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventCategories.Models;

namespace AlasApp.Application.EventCategories.Commands.UpdateEventCategories;

public sealed record UpdateEventCategoriesCommand(
    Guid EventId,
    bool UseCircuitTariffs,
    IReadOnlyCollection<EventCategoryUpsertItem> Categories) : IRequest<EventCategoryListDto>;

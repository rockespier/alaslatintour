using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.EventCategories.Models;

namespace AlasApp.Application.EventCategories.Queries.GetEventCategories;

public sealed record GetEventCategoriesQuery(Guid EventId) : IRequest<EventCategoryListDto>;

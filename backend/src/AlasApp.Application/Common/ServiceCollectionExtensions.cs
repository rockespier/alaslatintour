using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Categories.Commands.CreateCategory;
using AlasApp.Application.Categories.Commands.DeleteCategory;
using AlasApp.Application.Categories.Commands.UpdateCategory;
using AlasApp.Application.Categories.Queries.GetCategoryById;
using AlasApp.Application.Categories.Queries.ListCategories;
using AlasApp.Application.Circuits.Commands.CreateCircuit;
using AlasApp.Application.Circuits.Commands.DeleteCircuit;
using AlasApp.Application.Circuits.Commands.UpdateCircuit;
using AlasApp.Application.Circuits.Queries.GetCircuitById;
using AlasApp.Application.Circuits.Queries.ListCircuits;
using AlasApp.Application.EventCategories.Commands.UpdateEventCategories;
using AlasApp.Application.EventCategories.Queries.GetEventCategories;
using AlasApp.Application.Events.Commands.CreateEvent;
using AlasApp.Application.Events.Commands.DeleteEvent;
using AlasApp.Application.Events.Commands.UpdateEvent;
using AlasApp.Application.Events.Queries.GetEventById;
using AlasApp.Application.Events.Queries.ListEvents;
using Microsoft.Extensions.DependencyInjection;

namespace AlasApp.Application.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();

        services.AddScoped<IRequestHandler<ListCircuitsQuery, PagedResult<Circuits.Models.CircuitDto>>, ListCircuitsQueryHandler>();
        services.AddScoped<IRequestHandler<GetCircuitByIdQuery, Circuits.Models.CircuitDto>, GetCircuitByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateCircuitCommand, Circuits.Models.CircuitDto>, CreateCircuitCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateCircuitCommand, Circuits.Models.CircuitDto>, UpdateCircuitCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteCircuitCommand, bool>, DeleteCircuitCommandHandler>();

        services.AddScoped<IRequestHandler<ListEventsQuery, PagedResult<Events.Models.EventDto>>, ListEventsQueryHandler>();
        services.AddScoped<IRequestHandler<GetEventByIdQuery, Events.Models.EventDto>, GetEventByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateEventCommand, Events.Models.EventDto>, CreateEventCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateEventCommand, Events.Models.EventDto>, UpdateEventCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteEventCommand, bool>, DeleteEventCommandHandler>();

        services.AddScoped<IRequestHandler<ListCategoriesQuery, IReadOnlyCollection<Categories.Models.CategoryDto>>, ListCategoriesQueryHandler>();
        services.AddScoped<IRequestHandler<GetCategoryByIdQuery, Categories.Models.CategoryDto>, GetCategoryByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateCategoryCommand, Categories.Models.CategoryDto>, CreateCategoryCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateCategoryCommand, Categories.Models.CategoryDto>, UpdateCategoryCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteCategoryCommand, bool>, DeleteCategoryCommandHandler>();

        services.AddScoped<IRequestHandler<GetEventCategoriesQuery, EventCategories.Models.EventCategoryListDto>, GetEventCategoriesQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateEventCategoriesCommand, EventCategories.Models.EventCategoryListDto>, UpdateEventCategoriesCommandHandler>();

        return services;
    }
}

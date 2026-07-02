using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Events.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(AlasAppDbContext dbContext) : IEventRepository
{
    public async Task<PagedResult<EventDto>> ListAsync(EventListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;

        var query = dbContext.Events
            .AsNoTracking()
            .AsQueryable();

        if (filter.CircuitId.HasValue)
        {
            query = query.Where(x => x.CircuitId == filter.CircuitId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            query = query.Where(x => x.Pais == filter.Country.Trim());
        }

        if (filter.Stars.HasValue)
        {
            query = query.Where(x => x.Stars == filter.Stars.Value);
        }

        var filteredEvents = await query.ToListAsync(cancellationToken);

        if (filter.Year.HasValue)
        {
            filteredEvents = filteredEvents
                .Where(x => x.FechaInicio.Year == filter.Year.Value)
                .ToList();
        }

        if (filter.Status.HasValue)
        {
            filteredEvents = filteredEvents
                .Where(x => MatchesPublicStatus(x, filter.Status.Value))
                .ToList();
        }

        filteredEvents = filteredEvents
            .OrderBy(x => x.FechaInicio)
            .ThenBy(x => x.Nombre)
            .ToList();

        var totalItems = filteredEvents.Count;
        var events = filteredEvents
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        return new PagedResult<EventDto>(
            events.Select(MapToDto).ToList(),
            page,
            limit,
            totalItems);
    }

    public async Task<EventDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await dbContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        return @event is null ? null : MapToDto(@event);
    }

    public Task<Event?> GetEntityByIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return dbContext.Events
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);
    }

    public Task AddAsync(Event @event, CancellationToken cancellationToken)
    {
        return dbContext.Events.AddAsync(@event, cancellationToken).AsTask();
    }

    public void Remove(Event @event)
    {
        dbContext.Events.Remove(@event);
    }

    private static bool MatchesPublicStatus(Event @event, EventStatusPublic status)
    {
        return status switch
        {
            EventStatusPublic.InscripcionesAbiertas => @event.Estado == EventStatusAdmin.Activo,
            EventStatusPublic.Proximamente => @event.Estado is EventStatusAdmin.Proximo or EventStatusAdmin.Borrador,
            EventStatusPublic.Completado => @event.Estado == EventStatusAdmin.Completado,
            EventStatusPublic.Cerrado => @event.Estado == EventStatusAdmin.Cancelado,
            _ => true
        };
    }

    private static EventDto MapToDto(Event @event)
    {
        return new EventDto(
            @event.Id,
            @event.CircuitId,
            @event.Nombre,
            @event.FechaInicio,
            @event.FechaFin,
            @event.Pais,
            @event.Ciudad,
            @event.Playa,
            @event.Stars,
            @event.CapacidadMaxima,
            @event.PrizeAmountUsd,
            @event.SurfScoresCode,
            @event.AccessType,
            @event.Estado,
            0,
            @event.GetPublicStatus(),
            @event.GetLugar(),
            @event.CreatedAtUtc,
            @event.UpdatedAtUtc);
    }
}

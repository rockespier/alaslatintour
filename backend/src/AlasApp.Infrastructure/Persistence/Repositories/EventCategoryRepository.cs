using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.EventCategories.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class EventCategoryRepository(AlasAppDbContext dbContext) : IEventCategoryRepository
{
    public async Task<EventCategoryListDto?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await dbContext.Events
            .AsNoTracking()
            .Include(x => x.Categories)
                .ThenInclude(x => x.Category)
                    .ThenInclude(x => x!.Tariffs)
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (@event is null)
        {
            return null;
        }

        var data = @event.Categories
            .OrderBy(x => x.Category!.Nombre)
            .Select(x =>
            {
                var tariff = x.Category!.Tariffs.FirstOrDefault(t => t.StarLevel == @event.Stars && t.Active);
                var effectiveTariffUsd = @event.UseCircuitTariffs
                    ? tariff?.Usd ?? 0m
                    : x.CustomTariffUsd ?? tariff?.Usd ?? 0m;
                var effectiveTariffCop = @event.UseCircuitTariffs
                    ? tariff?.Cop ?? 0m
                    : x.CustomTariffCop ?? tariff?.Cop ?? 0m;

                return new EventCategoryDto(
                    x.CategoryId,
                    x.Category.Nombre,
                    x.CustomTariffUsd,
                    x.CustomTariffCop,
                    x.Capacidad,
                    effectiveTariffUsd,
                    effectiveTariffCop,
                    0);
            })
            .ToList();

        return new EventCategoryListDto(@event.UseCircuitTariffs, data);
    }

    public async Task<IReadOnlyCollection<EventCategory>> BuildAssignmentsAsync(
        Guid eventId,
        IReadOnlyCollection<EventCategoryUpsertItem> items,
        CancellationToken cancellationToken)
    {
        var categoryIds = items.Select(x => x.CategoryId).ToList();

        var existingIds = await dbContext.Categories
            .Where(x => categoryIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missingIds = categoryIds.Except(existingIds).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException("Una o mas categorias no existen.");
        }

        return items
            .Select(x => EventCategory.Create(
                eventId,
                x.CategoryId,
                x.CustomTariffUsd,
                x.CustomTariffCop,
                x.Capacidad))
            .ToList();
    }
}

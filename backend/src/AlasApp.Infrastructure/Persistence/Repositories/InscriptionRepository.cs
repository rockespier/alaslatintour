using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class InscriptionRepository(AlasAppDbContext dbContext) : IInscriptionRepository
{
    public async Task<PagedResult<AdminInscriptionRowDto>> ListAdminAsync(AdminInscriptionListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;

        var query = BuildInscriptionDetailsQuery();

        if (filter.EventId.HasValue)
        {
            query = query.Where(x => x.Event.Id == filter.EventId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(x => x.Category.Id == filter.CategoryId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Inscription.EstadoAdmin == filter.Status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Inscription.InscripcionAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var mapped = items
            .Select((x, index) => new AdminInscriptionRowDto(
                x.Inscription.Id,
                ((page - 1) * limit + index + 1).ToString("000"),
                $"{x.Competitor.Nombre} {x.Competitor.Apellido}",
                x.Competitor.Pais,
                null,
                null,
                x.Category.Nombre,
                x.Inscription.InscripcionAt,
                x.Inscription.PaymentMethod,
                x.Inscription.MontoUsd,
                x.Inscription.EstadoAdmin,
                x.Competitor.Federacion,
                x.Competitor.LicenseNumber,
                x.Inscription.TransaccionId,
                x.Inscription.Notes))
            .ToList();

        return new PagedResult<AdminInscriptionRowDto>(mapped, page, limit, totalItems);
    }

    public async Task<InscriptionDto?> GetByIdAsync(Guid inscriptionId, CancellationToken cancellationToken)
    {
        var item = await dbContext.Inscriptions
            .AsNoTracking()
            .Where(x => x.Id == inscriptionId)
            .Join(dbContext.Competitors,
                inscription => inscription.CompetitorId,
                competitor => competitor.Id,
                (inscription, competitor) => new { inscription, competitor })
            .Join(dbContext.Events,
                left => left.inscription.EventId,
                @event => @event.Id,
                (left, @event) => new { left.inscription, left.competitor, @event })
            .Join(dbContext.Categories,
                left => left.inscription.CategoryId,
                category => category.Id,
                (left, category) => new { left.inscription, left.competitor, left.@event, category })
            .Join(dbContext.Circuits,
                left => left.@event.CircuitId,
                circuit => circuit.Id,
                (left, circuit) => new InscriptionDetails(left.inscription, left.competitor, left.@event, left.category, circuit))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? null : MapToDto(item);
    }

    public Task<Inscription?> GetEntityByIdAsync(Guid inscriptionId, CancellationToken cancellationToken)
    {
        return dbContext.Inscriptions.FirstOrDefaultAsync(x => x.Id == inscriptionId, cancellationToken);
    }

    public async Task<PagedResult<CompetitorInscriptionDto>> ListByCompetitorAsync(
        Guid competitorId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = BuildInscriptionDetailsQuery()
            .Where(x => x.Competitor.Id == competitorId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = normalized switch
            {
                "confirmado" => query.Where(x => x.Inscription.EstadoCompetidor == InscriptionStatusCompetitor.Confirmado),
                "pendiente" => query.Where(x => x.Inscription.EstadoCompetidor == InscriptionStatusCompetitor.Pendiente),
                "completado" => query.Where(x => x.Inscription.EstadoCompetidor == InscriptionStatusCompetitor.Completado),
                _ => query
            };
        }

        var items = await query
            .OrderByDescending(x => x.Inscription.InscripcionAt)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(x => new CompetitorInscriptionDto(
            x.Inscription.Id,
            x.Competitor.Id.ToString(),
            x.Event.Id.ToString(),
            x.Event.Nombre,
            x.Event.GetLugar(),
            x.Category.Id.ToString(),
            x.Category.Nombre,
            x.Circuit.Id.ToString(),
            x.Circuit.Nombre,
            x.Inscription.ShirtNumber,
            x.Inscription.PaymentMethod,
            x.Inscription.MontoUsd,
            x.Inscription.EstadoAdmin,
            x.Inscription.EstadoCompetidor,
            x.Inscription.Resultado,
            x.Inscription.TransaccionId,
            x.Inscription.InscripcionAt)).ToList();

        return new PagedResult<CompetitorInscriptionDto>(mapped, 1, 20, mapped.Count);
    }

    public async Task<IReadOnlyCollection<CompetitorCalendarEventDto>> ListCalendarByCompetitorAsync(Guid competitorId, CancellationToken cancellationToken)
    {
        var items = await BuildInscriptionDetailsQuery()
            .Where(x => x.Competitor.Id == competitorId)
            .OrderBy(x => x.Event.FechaInicio)
            .ToListAsync(cancellationToken);

        return items.Select(x => new CompetitorCalendarEventDto(
            x.Event.Id.ToString(),
            x.Event.Nombre,
            x.Event.GetLugar(),
            x.Event.FechaInicio,
            x.Event.FechaFin,
            x.Category.Nombre,
            x.Inscription.EstadoCompetidor switch
            {
                InscriptionStatusCompetitor.Confirmado => "confirmado",
                InscriptionStatusCompetitor.Completado => "completado",
                _ => "pendiente"
            },
            x.Event.Stars)).ToList();
    }

    public Task<bool> ExistsDuplicateAsync(Guid competitorId, Guid eventId, Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Inscriptions.AnyAsync(
            x => x.CompetitorId == competitorId && x.EventId == eventId && x.CategoryId == categoryId,
            cancellationToken);
    }

    public Task<int> CountByEventCategoryAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Inscriptions.CountAsync(x => x.EventId == eventId && x.CategoryId == categoryId, cancellationToken);
    }

    public async Task<InscriptionPricingContext?> GetPricingContextAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken)
    {
        var item = await dbContext.Events
            .AsNoTracking()
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (item is null)
        {
            return null;
        }

        var assignment = item.Categories.FirstOrDefault(x => x.CategoryId == categoryId);
        if (assignment is null)
        {
            return null;
        }

        decimal? circuitTariff = await dbContext.CategoryTariffs
            .AsNoTracking()
            .Where(x => x.CategoryId == categoryId && x.StarLevel == item.Stars && x.Active)
            .Select(x => (decimal?)x.Usd)
            .FirstOrDefaultAsync(cancellationToken);

        return new InscriptionPricingContext(
            eventId,
            categoryId,
            item.CircuitId,
            item.UseCircuitTariffs,
            item.Stars,
            assignment.Capacidad,
            assignment.CustomTariffUsd,
            circuitTariff);
    }

    public Task AddAsync(Inscription inscription, CancellationToken cancellationToken)
    {
        return dbContext.Inscriptions.AddAsync(inscription, cancellationToken).AsTask();
    }

    public void Remove(Inscription inscription)
    {
        dbContext.Inscriptions.Remove(inscription);
    }

    private IQueryable<InscriptionDetails> BuildInscriptionDetailsQuery()
    {
        return dbContext.Inscriptions
            .AsNoTracking()
            .Join(dbContext.Competitors,
                inscription => inscription.CompetitorId,
                competitor => competitor.Id,
                (inscription, competitor) => new { inscription, competitor })
            .Join(dbContext.Events,
                left => left.inscription.EventId,
                @event => @event.Id,
                (left, @event) => new { left.inscription, left.competitor, @event })
            .Join(dbContext.Categories,
                left => left.inscription.CategoryId,
                category => category.Id,
                (left, category) => new { left.inscription, left.competitor, left.@event, category })
            .Join(dbContext.Circuits,
                left => left.@event.CircuitId,
                circuit => circuit.Id,
                (left, circuit) => new InscriptionDetails(left.inscription, left.competitor, left.@event, left.category, circuit));
    }

    private static InscriptionDto MapToDto(InscriptionDetails item)
    {
        return new InscriptionDto(
            item.Inscription.Id,
            new InscriptionCompetitorDto(item.Competitor.Id, $"{item.Competitor.Nombre} {item.Competitor.Apellido}", item.Competitor.Pais),
            new InscriptionEventDto(item.Event.Id, item.Event.Nombre, item.Event.GetLugar()),
            new InscriptionCategoryDto(item.Category.Id, item.Category.Nombre),
            new InscriptionCircuitDto(item.Circuit.Id, item.Circuit.Nombre),
            item.Inscription.ShirtNumber,
            item.Inscription.PaymentMethod,
            item.Inscription.MontoUsd,
            item.Inscription.EstadoAdmin,
            item.Inscription.EstadoCompetidor,
            item.Inscription.Resultado,
            item.Inscription.TransaccionId,
            item.Inscription.InscripcionAt,
            item.Inscription.Notes);
    }

    private sealed record InscriptionDetails(
        Inscription Inscription,
        Competitor Competitor,
        Event Event,
        Category Category,
        Circuit Circuit);
}

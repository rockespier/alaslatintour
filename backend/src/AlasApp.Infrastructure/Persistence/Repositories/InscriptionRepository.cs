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

        var query = BuildInscriptionBaseQuery();

        if (filter.EventId.HasValue)
            query = query.Where(x => x.EventId == filter.EventId.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.EstadoAdmin == filter.Status.Value);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.InscripcionAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var mapped = items
            .Select((x, index) => new AdminInscriptionRowDto(
                x.Id,
                ((page - 1) * limit + index + 1).ToString("000"),
                $"{x.Competitor!.Nombre} {x.Competitor.Apellido}",
                x.Competitor.Pais,
                null,
                null,
                x.Category!.Nombre,
                x.InscripcionAt,
                x.PaymentMethod,
                x.MontoUsd,
                x.EstadoAdmin,
                x.Competitor.Federacion,
                x.Competitor.LicenseNumber,
                x.TransaccionId,
                x.Notes))
            .ToList();

        return new PagedResult<AdminInscriptionRowDto>(mapped, page, limit, totalItems);
    }

    public async Task<InscriptionDto?> GetByIdAsync(Guid inscriptionId, CancellationToken cancellationToken)
    {
        var item = await BuildInscriptionBaseQuery()
            .FirstOrDefaultAsync(x => x.Id == inscriptionId, cancellationToken);

        return item is null ? null : MapToDto(item);
    }

    public Task<Inscription?> GetEntityByIdAsync(Guid inscriptionId, CancellationToken cancellationToken)
    {
        return dbContext.Inscriptions.FirstOrDefaultAsync(x => x.Id == inscriptionId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Inscription>> ListEntitiesByEventCategoryAsync(
        Guid eventId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Inscriptions
            .Where(x => x.EventId == eventId && x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<CompetitorInscriptionDto>> ListByCompetitorAsync(
        Guid competitorId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = BuildInscriptionBaseQuery()
            .Where(x => x.CompetitorId == competitorId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            query = normalized switch
            {
                "confirmado" => query.Where(x => x.EstadoCompetidor == InscriptionStatusCompetitor.Confirmado),
                "pendiente" => query.Where(x => x.EstadoCompetidor == InscriptionStatusCompetitor.Pendiente),
                "completado" => query.Where(x => x.EstadoCompetidor == InscriptionStatusCompetitor.Completado),
                _ => query
            };
        }

        var items = await query
            .OrderByDescending(x => x.InscripcionAt)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(x => new CompetitorInscriptionDto(
            x.Id,
            x.Competitor!.Id.ToString(),
            x.Event!.Id.ToString(),
            x.Event.Nombre,
            x.Event.GetLugar(),
            x.Category!.Id.ToString(),
            x.Category.Nombre,
            x.Event.Circuit!.Id.ToString(),
            x.Event.Circuit.Nombre,
            x.ShirtNumber,
            x.PaymentMethod,
            x.BaseAmountUsd,
            x.AdministrativeFeeUsd > 0 ? x.AdministrativeFeeUsd : null,
            x.MontoUsd,
            x.EstadoAdmin,
            x.EstadoCompetidor,
            x.Resultado,
            x.TransaccionId,
            x.InscripcionAt)).ToList();

        return new PagedResult<CompetitorInscriptionDto>(mapped, 1, 20, mapped.Count);
    }

    public async Task<IReadOnlyCollection<CompetitorCalendarEventDto>> ListCalendarByCompetitorAsync(Guid competitorId, CancellationToken cancellationToken)
    {
        var items = await BuildInscriptionBaseQuery()
            .Where(x => x.CompetitorId == competitorId)
            .OrderBy(x => x.Event!.FechaInicio)
            .ToListAsync(cancellationToken);

        return items.Select(x => new CompetitorCalendarEventDto(
            x.Event!.Id.ToString(),
            x.Event.Nombre,
            x.Event.GetLugar(),
            x.Event.FechaInicio,
            x.Event.FechaFin,
            x.Category!.Nombre,
            x.EstadoCompetidor switch
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
                .ThenInclude(x => x.Category)
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
            assignment.Category!.Gender,
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

    private IQueryable<Inscription> BuildInscriptionBaseQuery()
    {
        return dbContext.Inscriptions
            .AsNoTracking()
            .Include(x => x.Competitor)
            .Include(x => x.Event).ThenInclude(e => e!.Circuit)
            .Include(x => x.Category);
    }

    private static InscriptionDto MapToDto(Inscription x)
    {
        return new InscriptionDto(
            x.Id,
            new InscriptionCompetitorDto(x.Competitor!.Id, $"{x.Competitor.Nombre} {x.Competitor.Apellido}", x.Competitor.Pais),
            new InscriptionEventDto(x.Event!.Id, x.Event.Nombre, x.Event.GetLugar()),
            new InscriptionCategoryDto(x.Category!.Id, x.Category.Nombre),
            new InscriptionCircuitDto(x.Event.Circuit!.Id, x.Event.Circuit.Nombre),
            x.ShirtNumber,
            x.PaymentMethod,
            x.BaseAmountUsd,
            x.AdministrativeFeeUsd > 0 ? x.AdministrativeFeeUsd : null,
            x.MontoUsd,
            x.EstadoAdmin,
            x.EstadoCompetidor,
            x.Resultado,
            x.TransaccionId,
            x.InscripcionAt,
            x.Notes);
    }
}

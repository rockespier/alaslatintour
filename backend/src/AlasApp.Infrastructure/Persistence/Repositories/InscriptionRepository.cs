using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Application.Rankings;
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

        var rankings = await LoadRankingPositionsAsync(items, cancellationToken);

        var mapped = items
            .Select((x, index) => new AdminInscriptionRowDto(
                x.Id,
                x.CompetitorId,
                ((page - 1) * limit + index + 1).ToString("000"),
                $"{x.Competitor!.Nombre} {x.Competitor.Apellido}",
                x.Competitor.Pais,
                rankings.GetValueOrDefault(x.Id).Previous,
                rankings.GetValueOrDefault(x.Id).Current,
                x.Category!.Nombre,
                x.Event!.Nombre,
                x.InscripcionAt,
                x.PaymentMethod,
                x.MembershipPlan,
                x.MembershipFeeUsd > 0 ? x.MembershipFeeUsd : null,
                x.MontoUsd,
                x.EstadoAdmin,
                x.Competitor.Federacion,
                x.Competitor.LicenseNumber,
                x.TransaccionId,
                x.Notes))
            .ToList();

        return new PagedResult<AdminInscriptionRowDto>(mapped, page, limit, totalItems);
    }

    public async Task<IReadOnlyCollection<ConfirmedInscriptionRowDto>> ListConfirmedPublicAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var items = await BuildInscriptionBaseQuery()
            .Where(x => x.EventId == eventId && x.EstadoAdmin == InscriptionStatusAdmin.Pagado)
            .OrderBy(x => x.Category!.Nombre)
            .ThenBy(x => x.Competitor!.Nombre)
            .ToListAsync(cancellationToken);

        return items
            .Select(x => new ConfirmedInscriptionRowDto(
                $"{x.Competitor!.Nombre} {x.Competitor.Apellido}",
                x.Competitor.Pais,
                x.Category!.Nombre))
            .ToList();
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

    public async Task<IReadOnlyCollection<Inscription>> ListEntitiesByGroupIdAsync(Guid inscriptionGroupId, CancellationToken cancellationToken)
    {
        return await dbContext.Inscriptions
            .Where(x => x.InscriptionGroupId == inscriptionGroupId)
            .ToListAsync(cancellationToken);
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
            x.MembershipPlan,
            x.MembershipFeeUsd > 0 ? x.MembershipFeeUsd : null,
            x.MontoUsd,
            x.EstadoAdmin,
            x.EstadoCompetidor,
            x.Resultado,
            x.TransaccionId,
            x.ReglamentoAceptado,
            x.RiesgosAceptados,
            x.UsoImagenAceptado,
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
            circuitTariff,
            assignment.Category.MembresiaAnualUsd,
            assignment.Category.MembresiaPorEventoUsd,
            assignment.Category.AgeRestriction,
            assignment.Category.MinAge,
            assignment.Category.MaxAge);
    }

    public async Task<IReadOnlyCollection<Guid>> ListRegisteredCategoryIdsAsync(Guid competitorId, Guid eventId, CancellationToken cancellationToken)
    {
        return await dbContext.Inscriptions
            .AsNoTracking()
            .Where(x => x.CompetitorId == competitorId && x.EventId == eventId)
            .Select(x => x.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Inscription>> ListEntitiesByCompetitorAndEventAsync(Guid competitorId, Guid eventId, CancellationToken cancellationToken)
    {
        return await dbContext.Inscriptions
            .Where(x => x.CompetitorId == competitorId && x.EventId == eventId)
            .ToListAsync(cancellationToken);
    }

    public async Task<MembershipPlanOption?> GetActiveMembershipPlanForEventAsync(Guid competitorId, Guid eventId, Guid circuitId, CancellationToken cancellationToken)
    {
        var hasAnual = await dbContext.Inscriptions.AsNoTracking().AnyAsync(
            x => x.CompetitorId == competitorId
                && x.MembershipPlan == MembershipPlanOption.Anual
                && x.EstadoAdmin == InscriptionStatusAdmin.Pagado
                && x.Event != null && x.Event.CircuitId == circuitId,
            cancellationToken);

        if (hasAnual)
        {
            return MembershipPlanOption.Anual;
        }

        var hasPorEvento = await dbContext.Inscriptions.AsNoTracking().AnyAsync(
            x => x.CompetitorId == competitorId
                && x.EventId == eventId
                && x.MembershipPlan == MembershipPlanOption.PorEvento
                && x.EstadoAdmin == InscriptionStatusAdmin.Pagado,
            cancellationToken);

        return hasPorEvento ? MembershipPlanOption.PorEvento : null;
    }

    public Task AddAsync(Inscription inscription, CancellationToken cancellationToken)
    {
        return dbContext.Inscriptions.AddAsync(inscription, cancellationToken).AsTask();
    }

    public void Remove(Inscription inscription)
    {
        dbContext.Inscriptions.Remove(inscription);
    }

    /// <summary>
    /// Position of each inscribed competitor in the latest ranking of the event's season (Current)
    /// and of the season before (Previous), for the inscription's category. Ranking entries only
    /// carry the competitor's name, so they are matched with <see cref="RankingNameMatcher"/>.
    /// For each season the snapshot of the event's own circuit wins; otherwise the most recent one.
    /// </summary>
    private async Task<Dictionary<Guid, (string? Previous, string? Current)>> LoadRankingPositionsAsync(
        IReadOnlyCollection<Inscription> items,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, (string? Previous, string? Current)>();
        if (items.Count == 0)
        {
            return result;
        }

        static int SeasonOf(Inscription x) => x.Event!.Circuit?.Temporada ?? x.Event.FechaInicio.Year;

        var categoryIds = items.Select(x => x.CategoryId).Distinct().ToList();
        var years = items.SelectMany(x => new[] { SeasonOf(x), SeasonOf(x) - 1 }).Distinct().ToList();

        var snapshots = await dbContext.RankingSnapshots
            .AsNoTracking()
            .Where(s => categoryIds.Contains(s.CategoryId) && years.Contains(s.Year))
            .Select(s => new { s.Id, s.CircuitId, s.CategoryId, s.Year, s.CachedAtUtc })
            .ToListAsync(cancellationToken);
        if (snapshots.Count == 0)
        {
            return result;
        }

        Guid? PickSnapshot(Guid categoryId, int year, Guid preferredCircuitId) => snapshots
            .Where(s => s.CategoryId == categoryId && s.Year == year)
            .OrderByDescending(s => s.CircuitId == preferredCircuitId)
            .ThenByDescending(s => s.CachedAtUtc)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefault();

        var picks = items.ToDictionary(
            x => x.Id,
            x => (Previous: PickSnapshot(x.CategoryId, SeasonOf(x) - 1, x.Event!.CircuitId),
                  Current: PickSnapshot(x.CategoryId, SeasonOf(x), x.Event!.CircuitId)));

        var snapshotIds = picks.Values
            .SelectMany(p => new[] { p.Previous, p.Current })
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var entries = await dbContext.RankingSnapshotEntries
            .AsNoTracking()
            .Where(e => snapshotIds.Contains(e.RankingSnapshotId))
            .Select(e => new { e.RankingSnapshotId, e.CompetitorName, e.Position })
            .ToListAsync(cancellationToken);

        var matchers = entries
            .GroupBy(e => e.RankingSnapshotId)
            .ToDictionary(g => g.Key, g => new RankingNameMatcher(g.Select(e => (e.CompetitorName, e.Position))));

        string? PositionIn(Guid? snapshotId, Competitor competitor) =>
            snapshotId is { } id && matchers.TryGetValue(id, out var matcher)
                ? matcher.FindPosition(competitor.Nombre, competitor.Apellido)?.ToString()
                : null;

        foreach (var item in items)
        {
            var (previous, current) = picks[item.Id];
            result[item.Id] = (PositionIn(previous, item.Competitor!), PositionIn(current, item.Competitor!));
        }

        return result;
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
            x.MembershipPlan,
            x.MembershipFeeUsd > 0 ? x.MembershipFeeUsd : null,
            x.MontoUsd,
            x.EstadoAdmin,
            x.EstadoCompetidor,
            x.Resultado,
            x.TransaccionId,
            x.ReglamentoAceptado,
            x.RiesgosAceptados,
            x.UsoImagenAceptado,
            x.InscripcionAt,
            x.Notes);
    }
}

using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Dashboard.Models;
using AlasApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class AdminDashboardRepository(AlasAppDbContext dbContext) : IAdminDashboardRepository
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(new DateTime(now.Year, now.Month, 1), TimeSpan.Zero);
        var activeStatuses = new[] { EventStatusAdmin.Activo, EventStatusAdmin.Proximo };

        var totalCompetidores = await dbContext.Competitors.CountAsync(cancellationToken);
        var totalEventosActivos = await dbContext.Events.CountAsync(x => activeStatuses.Contains(x.Estado), cancellationToken);
        var totalInscripciones = await dbContext.Inscriptions.CountAsync(cancellationToken);
        var tokensPendientes = await dbContext.BeachTokens.CountAsync(x => x.Status == TokenHistoryStatus.Pendiente, cancellationToken);
        var recaudacionMesUsd = await dbContext.Payments
            .Where(x => x.Status == PaymentStatusAdmin.Confirmado && x.Fecha >= monthStart)
            .SumAsync(x => (decimal?)x.AmountUsd, cancellationToken) ?? 0m;

        var activeEvents = await dbContext.Events
            .AsNoTracking()
            .Where(x => activeStatuses.Contains(x.Estado))
            .OrderBy(x => x.FechaInicio)
            .Select(x => new DashboardActiveEventDto(
                x.Id,
                x.Nombre,
                DateOnly.FromDateTime(x.FechaInicio.UtcDateTime.Date),
                x.Estado,
                dbContext.Inscriptions.Count(i => i.EventId == x.Id)))
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentInscriptions = await dbContext.Inscriptions
            .AsNoTracking()
            .OrderByDescending(x => x.InscripcionAt)
            .Select(x => new DashboardRecentInscriptionDto(
                x.Competitor != null ? $"{x.Competitor.Nombre} {x.Competitor.Apellido}".Trim() : string.Empty,
                x.Event != null ? x.Event.Nombre : string.Empty,
                x.Category != null ? x.Category.Nombre : string.Empty,
                x.InscripcionAt))
            .Take(10)
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            new DashboardKpiDto(
                totalCompetidores,
                totalEventosActivos,
                totalInscripciones,
                recaudacionMesUsd,
                tokensPendientes),
            activeEvents,
            recentInscriptions);
    }
}

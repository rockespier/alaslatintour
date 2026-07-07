using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class BeachTokenRepository(AlasAppDbContext dbContext) : IBeachTokenRepository
{
    public Task<bool> HasActiveRequestAsync(Guid inscriptionId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return dbContext.BeachTokens
            .AsNoTracking()
            .AnyAsync(
                x => x.InscriptionId == inscriptionId
                    && x.Status == TokenHistoryStatus.Pendiente
                    && (!x.ExpirationAt.HasValue || x.ExpirationAt > utcNow),
                cancellationToken);
    }

    public Task<BeachToken?> GetEntityByIdAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return dbContext.BeachTokens.FirstOrDefaultAsync(x => x.Id == tokenId, cancellationToken);
    }

    public async Task<BeachTokenAdminDto?> GetAdminByIdAsync(Guid tokenId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var item = await BuildBeachTokenDetailsQuery()
            .FirstOrDefaultAsync(x => x.Token.Id == tokenId, cancellationToken);

        return item is null ? null : MapToDto(item, utcNow);
    }

    public Task<BeachToken?> GetLatestByInscriptionIdAsync(Guid inscriptionId, CancellationToken cancellationToken)
    {
        return dbContext.BeachTokens
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.InscriptionId == inscriptionId, cancellationToken);
    }

    public Task<BeachToken?> GetByTokenCodeAsync(string tokenCode, CancellationToken cancellationToken)
    {
        return dbContext.BeachTokens.FirstOrDefaultAsync(x => x.TokenCode == tokenCode, cancellationToken);
    }

    public Task AddAsync(BeachToken beachToken, CancellationToken cancellationToken)
    {
        return dbContext.BeachTokens.AddAsync(beachToken, cancellationToken).AsTask();
    }

    public async Task<BeachTokenAdminListDto> ListAdminAsync(int page, int limit, TokenHistoryStatus? status, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        page = page <= 0 ? 1 : page;
        limit = limit <= 0 ? 20 : limit;

        var baseQuery = BuildBeachTokenDetailsQuery();
        var pendingQuery = baseQuery.Where(x => ResolveStatus(x.Token, utcNow) == TokenHistoryStatus.Pendiente);
        var historyQuery = baseQuery.Where(x => ResolveStatus(x.Token, utcNow) != TokenHistoryStatus.Pendiente);

        IReadOnlyCollection<BeachTokenAdminDto> pending;
        IReadOnlyCollection<BeachTokenAdminDto> history;
        int totalItems;
        int totalPages;

        if (status == TokenHistoryStatus.Pendiente)
        {
            totalItems = await pendingQuery.CountAsync(cancellationToken);
            totalPages = (int)Math.Ceiling(totalItems / (double)limit);
            pending = (await pendingQuery
                .OrderByDescending(x => x.Token.CreatedAtUtc)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken))
                .Select(x => MapToDto(x, utcNow))
                .ToList();
            history = [];
        }
        else if (status.HasValue)
        {
            historyQuery = historyQuery.Where(x => ResolveStatus(x.Token, utcNow) == status.Value);
            totalItems = await historyQuery.CountAsync(cancellationToken);
            totalPages = (int)Math.Ceiling(totalItems / (double)limit);
            history = (await historyQuery
                .OrderByDescending(x => x.Token.UpdatedAtUtc)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken))
                .Select(x => MapToDto(x, utcNow))
                .ToList();
            pending = [];
        }
        else
        {
            totalItems = await historyQuery.CountAsync(cancellationToken);
            totalPages = (int)Math.Ceiling(totalItems / (double)limit);
            pending = (await pendingQuery
                .OrderByDescending(x => x.Token.CreatedAtUtc)
                .ToListAsync(cancellationToken))
                .Select(x => MapToDto(x, utcNow))
                .ToList();
            history = (await historyQuery
                .OrderByDescending(x => x.Token.UpdatedAtUtc)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken))
                .Select(x => MapToDto(x, utcNow))
                .ToList();
        }

        var todayStart = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero);
        var tomorrowStart = todayStart.AddDays(1);

        var allItems = await baseQuery.ToListAsync(cancellationToken);
        var pendingCount = allItems.Count(x => ResolveStatus(x.Token, utcNow) == TokenHistoryStatus.Pendiente);
        var approvedToday = allItems.Count(x => x.Token.GeneratedAt.HasValue && x.Token.GeneratedAt.Value >= todayStart && x.Token.GeneratedAt.Value < tomorrowStart);
        var rejectedToday = allItems.Count(x => x.Token.Status == TokenHistoryStatus.Rechazado && x.Token.UpdatedAtUtc >= todayStart && x.Token.UpdatedAtUtc < tomorrowStart);

        return new BeachTokenAdminListDto(
            pending is List<BeachTokenAdminDto> pendingList ? pendingList : pending.ToList(),
            history is List<BeachTokenAdminDto> historyList ? historyList : history.ToList(),
            new BeachTokenDailyStatsDto(pendingCount, approvedToday, rejectedToday),
            new PaginationMetaDto(page, totalPages == 0 ? 1 : totalPages, totalItems, limit));
    }

    private IQueryable<BeachTokenDetails> BuildBeachTokenDetailsQuery()
    {
        return dbContext.BeachTokens
            .AsNoTracking()
            .Join(dbContext.Inscriptions,
                token => token.InscriptionId,
                inscription => inscription.Id,
                (token, inscription) => new { token, inscription })
            .Join(dbContext.Competitors,
                left => left.inscription.CompetitorId,
                competitor => competitor.Id,
                (left, competitor) => new { left.token, left.inscription, competitor })
            .Join(dbContext.Events,
                left => left.inscription.EventId,
                @event => @event.Id,
                (left, @event) => new { left.token, left.inscription, left.competitor, @event })
            .Join(dbContext.Categories,
                left => left.inscription.CategoryId,
                category => category.Id,
                (left, category) => new BeachTokenDetails(left.token, left.inscription, left.competitor, left.@event, category));
    }

    private static BeachTokenAdminDto MapToDto(BeachTokenDetails item, DateTimeOffset utcNow)
    {
        return new BeachTokenAdminDto(
            item.Token.Id,
            $"{item.Competitor.Nombre} {item.Competitor.Apellido}",
            item.Competitor.Email,
            item.Event.Nombre,
            item.Category.Nombre,
            item.Inscription.MontoUsd,
            item.Token.TokenCode,
            ResolveStatus(item.Token, utcNow),
            item.Token.GeneratedAt,
            item.Token.ExpirationAt,
            item.Token.UsedAt);
    }

    private static TokenHistoryStatus ResolveStatus(BeachToken token, DateTimeOffset utcNow)
    {
        if (token.Status == TokenHistoryStatus.Pendiente && token.ExpirationAt.HasValue && token.ExpirationAt.Value <= utcNow)
        {
            return TokenHistoryStatus.Expirado;
        }

        return token.Status;
    }

    private sealed record BeachTokenDetails(
        BeachToken Token,
        Inscription Inscription,
        Competitor Competitor,
        Event Event,
        Category Category);
}

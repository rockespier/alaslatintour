using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.CompetitorFines.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class CompetitorFineRepository(AlasAppDbContext dbContext) : ICompetitorFineRepository
{
    public async Task<IReadOnlyCollection<CompetitorFineDto>> ListByCompetitorAsync(Guid competitorId, CancellationToken cancellationToken)
    {
        return await dbContext.CompetitorFines
            .AsNoTracking()
            .Where(x => x.CompetitorId == competitorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    public Task<CompetitorFineDto?> GetByIdAsync(Guid fineId, CancellationToken cancellationToken)
    {
        return dbContext.CompetitorFines
            .AsNoTracking()
            .Where(x => x.Id == fineId)
            .Select(MapToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CompetitorFine?> GetEntityByIdAsync(Guid fineId, CancellationToken cancellationToken)
    {
        return dbContext.CompetitorFines.FirstOrDefaultAsync(x => x.Id == fineId, cancellationToken);
    }

    public Task AddAsync(CompetitorFine fine, CancellationToken cancellationToken)
    {
        return dbContext.CompetitorFines.AddAsync(fine, cancellationToken).AsTask();
    }

    private static Expression<Func<CompetitorFine, CompetitorFineDto>> MapToDto()
    {
        return x => new CompetitorFineDto(
            x.Id,
            x.CompetitorId,
            x.AmountUsd,
            x.Reason,
            x.Notes,
            x.Status,
            x.CreatedByUserId,
            x.CreatedAtUtc,
            x.UpdatedAtUtc);
    }
}

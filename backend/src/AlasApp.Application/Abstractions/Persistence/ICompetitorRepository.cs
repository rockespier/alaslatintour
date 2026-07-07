using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface ICompetitorRepository
{
    Task<PagedResult<CompetitorDto>> ListAsync(CompetitorListFilter filter, CancellationToken cancellationToken);

    Task<CompetitorDto?> GetByIdAsync(Guid competitorId, CancellationToken cancellationToken);

    Task<Competitor?> GetEntityByIdAsync(Guid competitorId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? excludedCompetitorId, CancellationToken cancellationToken);

    Task AddAsync(Competitor competitor, CancellationToken cancellationToken);

    void Remove(Competitor competitor);
}

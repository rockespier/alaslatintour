using AlasApp.Application.CompetitorFines.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface ICompetitorFineRepository
{
    Task<IReadOnlyCollection<CompetitorFineDto>> ListByCompetitorAsync(Guid competitorId, CancellationToken cancellationToken);

    Task<CompetitorFineDto?> GetByIdAsync(Guid fineId, CancellationToken cancellationToken);

    Task<CompetitorFine?> GetEntityByIdAsync(Guid fineId, CancellationToken cancellationToken);

    Task AddAsync(CompetitorFine fine, CancellationToken cancellationToken);
}

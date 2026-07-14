using AlasApp.Application.Common;
using AlasApp.Application.Inscriptions.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IInscriptionRepository
{
    Task<PagedResult<AdminInscriptionRowDto>> ListAdminAsync(AdminInscriptionListFilter filter, CancellationToken cancellationToken);

    Task<InscriptionDto?> GetByIdAsync(Guid inscriptionId, CancellationToken cancellationToken);

    Task<Inscription?> GetEntityByIdAsync(Guid inscriptionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Inscription>> ListEntitiesByEventCategoryAsync(
        Guid eventId,
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<PagedResult<Competitors.Models.CompetitorInscriptionDto>> ListByCompetitorAsync(
        Guid competitorId,
        string? status,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Competitors.Models.CompetitorCalendarEventDto>> ListCalendarByCompetitorAsync(
        Guid competitorId,
        CancellationToken cancellationToken);

    Task<bool> ExistsDuplicateAsync(Guid competitorId, Guid eventId, Guid categoryId, CancellationToken cancellationToken);

    Task<int> CountByEventCategoryAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken);

    Task<InscriptionPricingContext?> GetPricingContextAsync(Guid eventId, Guid categoryId, CancellationToken cancellationToken);

    Task AddAsync(Inscription inscription, CancellationToken cancellationToken);

    void Remove(Inscription inscription);
}

using AlasApp.Application.Payments.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;

namespace AlasApp.Application.Abstractions.Persistence;

public interface IBeachTokenRepository
{
    Task<bool> HasActiveRequestAsync(Guid inscriptionId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<BeachToken?> GetEntityByIdAsync(Guid tokenId, CancellationToken cancellationToken);

    Task<BeachTokenAdminDto?> GetAdminByIdAsync(Guid tokenId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<BeachToken?> GetLatestByInscriptionIdAsync(Guid inscriptionId, CancellationToken cancellationToken);

    Task<BeachToken?> GetByTokenCodeAsync(string tokenCode, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BeachToken>> ListByInscriptionIdAsync(Guid inscriptionId, CancellationToken cancellationToken);

    Task AddAsync(BeachToken beachToken, CancellationToken cancellationToken);

    void RemoveRange(IEnumerable<BeachToken> tokens);

    Task<BeachTokenAdminListDto> ListAdminAsync(int page, int limit, TokenHistoryStatus? status, DateTimeOffset utcNow, CancellationToken cancellationToken);
}

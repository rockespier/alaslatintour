using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface ICircuitRepository
{
    Task<PagedResult<CircuitDto>> ListAsync(CircuitListFilter filter, CancellationToken cancellationToken);

    Task<CircuitDto?> GetByIdAsync(Guid circuitId, CancellationToken cancellationToken);

    Task<Circuit?> GetEntityByIdAsync(Guid circuitId, CancellationToken cancellationToken);

    Task AddAsync(Circuit circuit, CancellationToken cancellationToken);

    Task<bool> HasEventsAsync(Guid circuitId, CancellationToken cancellationToken);

    void Remove(Circuit circuit);
}

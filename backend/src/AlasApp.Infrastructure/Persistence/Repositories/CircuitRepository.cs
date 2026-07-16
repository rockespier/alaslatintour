using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Circuits.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class CircuitRepository(AlasAppDbContext dbContext) : ICircuitRepository
{
    public async Task<PagedResult<CircuitDto>> ListAsync(CircuitListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;

        var query = dbContext.Circuits
            .AsNoTracking()
            .Include(x => x.Events)
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Estado == filter.Status.Value);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(x => x.Temporada == filter.Year.Value);
        }

        if (filter.Modalidad.HasValue)
        {
            query = query.Where(x => x.Modalidad == filter.Modalidad.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var circuits = await query
            .OrderByDescending(x => x.Temporada)
            .ThenBy(x => x.Nombre)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<CircuitDto>(
            circuits.Select(MapToDto).ToList(),
            page,
            limit,
            totalItems);
    }

    public async Task<CircuitDto?> GetByIdAsync(Guid circuitId, CancellationToken cancellationToken)
    {
        var circuit = await dbContext.Circuits
            .AsNoTracking()
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == circuitId, cancellationToken);

        return circuit is null ? null : MapToDto(circuit);
    }

    public Task<Circuit?> GetEntityByIdAsync(Guid circuitId, CancellationToken cancellationToken)
    {
        return dbContext.Circuits
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == circuitId, cancellationToken);
    }

    public Task<Circuit?> GetEntityBySurfScoresCodeAsync(string surfScoresCode, CancellationToken cancellationToken)
    {
        var normalizedCode = surfScoresCode.Trim();

        return dbContext.Circuits
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.SurfScoresCode == normalizedCode, cancellationToken);
    }

    public Task<Circuit?> GetEntityByNameAndSeasonAsync(string nombre, int temporada, CancellationToken cancellationToken)
    {
        var normalizedName = nombre.Trim();

        return dbContext.Circuits
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Nombre == normalizedName && x.Temporada == temporada, cancellationToken);
    }

    public Task<Circuit?> GetCurrentBySeasonAsync(int seasonYear, CancellationToken cancellationToken)
    {
        return dbContext.Circuits
            .AsNoTracking()
            .Where(x => x.Temporada == seasonYear)
            .OrderBy(x => x.Estado == Domain.Enums.CircuitStatus.Activo ? 0 : x.Estado == Domain.Enums.CircuitStatus.Proximo ? 1 : 2)
            .ThenByDescending(x => x.LastSyncAt ?? x.UpdatedAtUtc)
            .ThenBy(x => x.Nombre)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        return dbContext.Circuits.AddAsync(circuit, cancellationToken).AsTask();
    }

    public Task<bool> HasEventsAsync(Guid circuitId, CancellationToken cancellationToken)
    {
        return dbContext.Events.AnyAsync(x => x.CircuitId == circuitId, cancellationToken);
    }

    public void Remove(Circuit circuit)
    {
        dbContext.Circuits.Remove(circuit);
    }

    private static CircuitDto MapToDto(Circuit circuit)
    {
        return new CircuitDto(
            circuit.Id,
            circuit.Nombre,
            circuit.Temporada,
            circuit.Descripcion,
            circuit.Region,
            circuit.Modalidad,
            circuit.Estado,
            circuit.SurfScoresCode,
            circuit.Events.Count,
            0,
            circuit.Events.Sum(x => x.PrizeAmountUsd),
            circuit.LastSyncAt,
            circuit.CreatedAtUtc,
            circuit.UpdatedAtUtc);
    }
}

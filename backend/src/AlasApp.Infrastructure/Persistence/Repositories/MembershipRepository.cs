using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Memberships.Models;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class MembershipRepository(AlasAppDbContext dbContext) : IMembershipRepository
{
    public async Task<PagedResult<MembershipDto>> ListAsync(MembershipListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;
        var warningDate = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(30), TimeSpan.Zero);

        var query = dbContext.Memberships
            .AsNoTracking()
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = filter.Status.Value switch
            {
                MembershipStatus.Activo => query.Where(x => x.Vencimiento > warningDate),
                MembershipStatus.VencePronto => query.Where(x => x.Vencimiento <= warningDate),
                _ => query
            };
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new MembershipRow(
                x.Id,
                x.ClubFederacion,
                x.Pais,
                x.Plan,
                x.InicioVigencia,
                x.Vencimiento,
                x.EmailContacto,
                x.CreatedAtUtc,
                dbContext.Competitors.Count(c =>
                    c.Pais == x.Pais &&
                    (c.Club == x.ClubFederacion || c.Federacion == x.ClubFederacion))))
            .ToListAsync(cancellationToken);

        return new PagedResult<MembershipDto>(
            rows.Select(MapToDto).ToList(),
            page,
            limit,
            totalItems);
    }

    public async Task<MembershipDto?> GetByIdAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Memberships
            .AsNoTracking()
            .Where(x => x.Id == membershipId)
            .Select(x => new MembershipRow(
                x.Id,
                x.ClubFederacion,
                x.Pais,
                x.Plan,
                x.InicioVigencia,
                x.Vencimiento,
                x.EmailContacto,
                x.CreatedAtUtc,
                dbContext.Competitors.Count(c =>
                    c.Pais == x.Pais &&
                    (c.Club == x.ClubFederacion || c.Federacion == x.ClubFederacion))))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapToDto(row);
    }

    public Task<Membership?> GetEntityByIdAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        return dbContext.Memberships.FirstOrDefaultAsync(x => x.Id == membershipId, cancellationToken);
    }

    public Task AddAsync(Membership membership, CancellationToken cancellationToken)
    {
        return dbContext.Memberships.AddAsync(membership, cancellationToken).AsTask();
    }

    public void Remove(Membership membership)
    {
        dbContext.Memberships.Remove(membership);
    }

    private static MembershipDto MapToDto(MembershipRow row)
    {
        return new MembershipDto(
            row.Id,
            row.ClubFederacion,
            row.Pais,
            row.Plan,
            row.InicioVigencia,
            row.Vencimiento,
            row.EmailContacto,
            row.CompetidoresAfiliados,
            ResolveStatus(row.Vencimiento),
            row.CreatedAtUtc);
    }

    private static MembershipStatus ResolveStatus(DateTimeOffset vencimiento)
    {
        var warningDate = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(30), TimeSpan.Zero);
        return vencimiento <= warningDate
            ? MembershipStatus.VencePronto
            : MembershipStatus.Activo;
    }

    private sealed record MembershipRow(
        Guid Id,
        string ClubFederacion,
        string Pais,
        MembershipPlan Plan,
        DateTimeOffset InicioVigencia,
        DateTimeOffset Vencimiento,
        string EmailContacto,
        DateTimeOffset CreatedAtUtc,
        int CompetidoresAfiliados);
}

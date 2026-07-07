using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Application.Competitors.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class CompetitorRepository(AlasAppDbContext dbContext) : ICompetitorRepository
{
    public async Task<PagedResult<CompetitorDto>> ListAsync(CompetitorListFilter filter, CancellationToken cancellationToken)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var limit = filter.Limit <= 0 ? 20 : filter.Limit;

        var query = dbContext.Competitors
            .AsNoTracking()
            .Include(x => x.EnabledLicenseCategories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            var country = filter.Country.Trim();
            query = query.Where(x => x.Pais == country);
        }

        if (!string.IsNullOrWhiteSpace(filter.CategoryId))
        {
            var categoryId = filter.CategoryId.Trim();
            query = query.Where(x => x.EnabledLicenseCategories.Any(y => y.CategoryId == categoryId));
        }

        if (filter.LicenseStatus.HasValue)
        {
            query = query.Where(x => x.LicenseStatus == filter.LicenseStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x =>
                x.Nombre.Contains(search) ||
                x.Apellido.Contains(search) ||
                x.LicenseNumber.Contains(search) ||
                x.LicenseNumberLong.Contains(search));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var competitors = await query
            .OrderBy(x => x.Apellido)
            .ThenBy(x => x.Nombre)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<CompetitorDto>(
            competitors.Select(MapToDto).ToList(),
            page,
            limit,
            totalItems);
    }

    public async Task<CompetitorDto?> GetByIdAsync(Guid competitorId, CancellationToken cancellationToken)
    {
        var competitor = await dbContext.Competitors
            .AsNoTracking()
            .Include(x => x.EnabledLicenseCategories)
            .FirstOrDefaultAsync(x => x.Id == competitorId, cancellationToken);

        return competitor is null ? null : MapToDto(competitor);
    }

    public Task<Competitor?> GetEntityByIdAsync(Guid competitorId, CancellationToken cancellationToken)
    {
        return dbContext.Competitors
            .Include(x => x.EnabledLicenseCategories)
            .FirstOrDefaultAsync(x => x.Id == competitorId, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludedCompetitorId, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();

        return dbContext.Competitors.AnyAsync(
            x => x.Email == normalizedEmail && (!excludedCompetitorId.HasValue || x.Id != excludedCompetitorId.Value),
            cancellationToken);
    }

    public Task AddAsync(Competitor competitor, CancellationToken cancellationToken)
    {
        return dbContext.Competitors.AddAsync(competitor, cancellationToken).AsTask();
    }

    public void Remove(Competitor competitor)
    {
        dbContext.Competitors.Remove(competitor);
    }

    private static CompetitorDto MapToDto(Competitor competitor)
    {
        return new CompetitorDto(
            competitor.Id,
            competitor.Nombre,
            competitor.Apellido,
            competitor.Email,
            competitor.FechaNacimiento,
            competitor.Genero,
            competitor.Pais,
            competitor.Telefono,
            competitor.Club,
            competitor.Postura,
            competitor.TallaCamiseta,
            competitor.NumeroCamiseta,
            competitor.Patrocinadores,
            competitor.Federacion,
            competitor.SurfScoresCode,
            new CompetitorLicenseDto(
                competitor.LicenseNumber,
                competitor.LicenseNumberLong,
                competitor.LicenseStatus,
                competitor.LicenseExpirationDate,
                competitor.EnabledLicenseCategories.Select(x => x.CategoryId).ToList()),
            competitor.NotificationEmail,
            competitor.NotificationPush,
            competitor.NotificationResultados,
            competitor.NotificationInscripciones,
            competitor.CreatedAtUtc);
    }
}

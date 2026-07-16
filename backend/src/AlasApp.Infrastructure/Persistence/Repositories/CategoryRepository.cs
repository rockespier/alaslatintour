using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Categories.Models;
using AlasApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlasApp.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AlasAppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyCollection<CategoryDto>> ListAsync(CategoryListFilter filter, CancellationToken cancellationToken)
    {
        var query = dbContext.Categories
            .AsNoTracking()
            .Include(x => x.SuccessorCategory)
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        var categories = await query
            .OrderBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Include(x => x.SuccessorCategory)
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);

        return category is null ? null : MapToDto(category);
    }

    public Task<Category?> GetEntityByIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Categories
            .Include(x => x.Tariffs)
            .Include(x => x.EventCategories)
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
    }

    public Task<Category?> GetEntityBySurfScoresCodeAsync(string surfScoresCode, CancellationToken cancellationToken)
    {
        var normalizedCode = surfScoresCode.Trim();

        return dbContext.Categories
            .Include(x => x.Tariffs)
            .Include(x => x.EventCategories)
            .FirstOrDefaultAsync(x => x.SurfScoresCode == normalizedCode, cancellationToken);
    }

    public Task<Category?> GetEntityByNameAsync(string nombre, CancellationToken cancellationToken)
    {
        var normalizedName = nombre.Trim();

        return dbContext.Categories
            .Include(x => x.Tariffs)
            .Include(x => x.EventCategories)
            .FirstOrDefaultAsync(x => x.Nombre == normalizedName, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AnyAsync(x => x.Id == categoryId, cancellationToken);
    }

    public Task<bool> HasSuccessorsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AnyAsync(x => x.SuccessorCategoryId == categoryId, cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AddAsync(category, cancellationToken).AsTask();
    }

    public void Remove(Category category)
    {
        dbContext.Categories.Remove(category);
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Nombre,
            category.Descripcion,
            category.Gender,
            category.AgeRestriction,
            category.MinAge,
            category.MaxAge,
            category.SuccessorCategoryId,
            category.SuccessorCategory is null
                ? null
                : new CategorySummaryDto(category.SuccessorCategory.Id, category.SuccessorCategory.Nombre),
            category.Status,
            category.MembresiaAnualUsd,
            category.MembresiaPorEventoUsd,
            category.BestResultsCount,
            category.CreatedAtUtc,
            category.SurfScoresCode);
    }
}

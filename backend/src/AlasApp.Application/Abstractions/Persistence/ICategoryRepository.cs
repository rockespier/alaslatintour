using AlasApp.Application.Categories.Models;
using AlasApp.Domain.Entities;

namespace AlasApp.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<CategoryDto>> ListAsync(CategoryListFilter filter, CancellationToken cancellationToken);

    Task<CategoryDto?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<Category?> GetEntityByIdAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<bool> HasSuccessorsAsync(Guid categoryId, CancellationToken cancellationToken);

    Task AddAsync(Category category, CancellationToken cancellationToken);

    void Remove(Category category);
}

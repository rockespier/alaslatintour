using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Categories.Models;

namespace AlasApp.Application.Categories.Queries.ListCategories;

public sealed class ListCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    public Task<IReadOnlyCollection<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        return categoryRepository.ListAsync(request.Filter, cancellationToken);
    }
}

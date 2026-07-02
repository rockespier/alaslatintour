using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Categories.Models;

namespace AlasApp.Application.Categories.Queries.ListCategories;

public sealed record ListCategoriesQuery(CategoryListFilter Filter) : IRequest<IReadOnlyCollection<CategoryDto>>;

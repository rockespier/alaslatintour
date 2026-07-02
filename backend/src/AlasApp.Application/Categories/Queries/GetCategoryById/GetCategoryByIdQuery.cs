using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Categories.Models;

namespace AlasApp.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryDto>;

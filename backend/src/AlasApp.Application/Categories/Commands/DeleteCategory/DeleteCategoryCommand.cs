using AlasApp.Application.Abstractions.Messaging;

namespace AlasApp.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest<bool>;

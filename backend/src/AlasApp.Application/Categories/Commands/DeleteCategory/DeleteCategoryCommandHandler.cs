using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("categoryId", "El identificador de la categoria es invalido.")]);
        }

        var category = await categoryRepository.GetEntityByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Categoria no encontrada.");

        if (await categoryRepository.HasSuccessorsAsync(request.CategoryId, cancellationToken))
        {
            throw new ConflictException("No se puede eliminar una categoria que es sucesora de otra.");
        }

        try
        {
            category.EnsureCanBeDeleted();
            categoryRepository.Remove(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DomainRuleException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }
}

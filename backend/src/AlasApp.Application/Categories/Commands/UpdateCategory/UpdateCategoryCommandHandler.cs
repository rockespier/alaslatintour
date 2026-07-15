using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Categories.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureSuccessorExistsAsync(request.SuccessorCategoryId, request.CategoryId, cancellationToken);

        var category = await categoryRepository.GetEntityByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Categoria no encontrada.");

        try
        {
            category.Update(
                request.Nombre,
                request.Descripcion,
                request.Gender,
                request.AgeRestriction,
                request.MinAge,
                request.MaxAge,
                request.SuccessorCategoryId,
                request.Status,
                request.SurfScoresCode);

            category.SetUpdated(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await categoryRepository.GetByIdAsync(category.Id, cancellationToken)
                ?? throw new NotFoundException("Categoria no encontrada despues de actualizarla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private async Task EnsureSuccessorExistsAsync(Guid? successorCategoryId, Guid categoryId, CancellationToken cancellationToken)
    {
        if (!successorCategoryId.HasValue)
        {
            return;
        }

        if (successorCategoryId.Value == categoryId)
        {
            throw new ValidationException(
                "La solicitud contiene errores de validacion.",
                [new ValidationError("successorCategoryId", "La categoria sucesora no puede ser la misma categoria.")]);
        }

        if (!await categoryRepository.ExistsAsync(successorCategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("La categoria sucesora no existe.");
        }
    }

    private static void Validate(UpdateCategoryCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add(new ValidationError("categoryId", "El identificador de la categoria es invalido."));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.Abstractions.Services;
using AlasApp.Application.Categories.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Entities;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureSuccessorExistsAsync(request.SuccessorCategoryId, cancellationToken);

        try
        {
            var category = Category.Create(
                request.Nombre,
                request.Descripcion,
                request.Gender,
                request.AgeRestriction,
                request.MinAge,
                request.MaxAge,
                request.SuccessorCategoryId,
                request.Status,
                request.MembresiaAnualUsd,
                request.MembresiaPorEventoUsd,
                request.BestResultsCount,
                request.SurfScoresCode);

            category.SetCreated(clock.UtcNow);

            await categoryRepository.AddAsync(category, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await categoryRepository.GetByIdAsync(category.Id, cancellationToken)
                ?? throw new NotFoundException("Categoria no encontrada despues de crearla.");
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private async Task EnsureSuccessorExistsAsync(Guid? successorCategoryId, CancellationToken cancellationToken)
    {
        if (!successorCategoryId.HasValue)
        {
            return;
        }

        if (!await categoryRepository.ExistsAsync(successorCategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("La categoria sucesora no existe.");
        }
    }

    private static void Validate(CreateCategoryCommand request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add(new ValidationError("nombre", "El nombre es obligatorio."));
        }

        if (request.MembresiaAnualUsd < 0)
        {
            errors.Add(new ValidationError("membresiaAnualUsd", "La membresia anual no puede ser negativa."));
        }

        if (request.MembresiaPorEventoUsd < 0)
        {
            errors.Add(new ValidationError("membresiaPorEventoUsd", "La membresia por evento no puede ser negativa."));
        }

        if (request.BestResultsCount is < 1 or > 10)
        {
            errors.Add(new ValidationError("bestResultsCount", "La cantidad de mejores resultados debe estar entre 1 y 10."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

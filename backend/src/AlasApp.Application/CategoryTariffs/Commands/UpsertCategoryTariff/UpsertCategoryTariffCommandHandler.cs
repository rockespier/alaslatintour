using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.CategoryTariffs.Models;
using AlasApp.Application.Common;
using AlasApp.Domain.Exceptions;

namespace AlasApp.Application.CategoryTariffs.Commands.UpsertCategoryTariff;

public sealed class UpsertCategoryTariffCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertCategoryTariffCommand, CategoryTariffDto>
{
    public async Task<CategoryTariffDto> Handle(UpsertCategoryTariffCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var category = await categoryRepository.GetEntityByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Categoria no encontrada.");

        try
        {
            var tariff = category.SetTariff(request.StarLevel, request.Usd, request.Cop, request.Active);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new CategoryTariffDto(tariff.StarLevel, tariff.Usd, tariff.Cop, tariff.Active);
        }
        catch (DomainRuleException exception)
        {
            throw new ValidationException(exception.Message, [new ValidationError("body", exception.Message)]);
        }
    }

    private static void Validate(UpsertCategoryTariffCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add(new ValidationError("categoryId", "El identificador de la categoria es invalido."));
        }

        if (request.StarLevel is < 1 or > 5)
        {
            errors.Add(new ValidationError("starLevel", "El nivel de estrellas debe estar entre 1 y 5."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("La solicitud contiene errores de validacion.", errors);
        }
    }
}

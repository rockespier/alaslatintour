using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.Abstractions.Persistence;
using AlasApp.Application.CategoryTariffs.Models;
using AlasApp.Application.Common;

namespace AlasApp.Application.CategoryTariffs.Queries.GetCategoryTariffs;

public sealed class GetCategoryTariffsQueryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoryTariffsQuery, IReadOnlyCollection<CategoryTariffDto>>
{
    public async Task<IReadOnlyCollection<CategoryTariffDto>> Handle(GetCategoryTariffsQuery request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetEntityByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Categoria no encontrada.");

        return Enumerable.Range(1, 5)
            .Select(starLevel =>
            {
                var tariff = category.Tariffs.FirstOrDefault(x => x.StarLevel == starLevel);
                return tariff is null
                    ? new CategoryTariffDto(starLevel, 0m, 0m, false)
                    : new CategoryTariffDto(tariff.StarLevel, tariff.Usd, tariff.Cop, tariff.Active);
            })
            .ToList();
    }
}

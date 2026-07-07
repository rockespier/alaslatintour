using AlasApp.Application.Abstractions.Messaging;
using AlasApp.Application.CategoryTariffs.Models;

namespace AlasApp.Application.CategoryTariffs.Queries.GetCategoryTariffs;

public sealed record GetCategoryTariffsQuery(Guid CategoryId) : IRequest<IReadOnlyCollection<CategoryTariffDto>>;
